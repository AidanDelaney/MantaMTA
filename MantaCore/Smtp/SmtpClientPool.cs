using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;
using MantaMTA.Core.Client;
using MantaMTA.Core.DNS;

namespace MantaMTA.Core.Smtp
{
	/// <summary>
	/// Is a Thread safe queue used to hold connected and unused SmtpOutboundClients.
	/// </summary>
	internal class SmtpClientQueue : ConcurrentQueue<SmtpOutboundClient> 
	{
		/// <summary>
		/// Object used for locking.
		/// </summary>
		public object SyncRoot = new object();
		/// <summary>
		/// Holds a list of currently active and inuse connections.
		/// </summary>
		public ArrayList InUseConnections = new ArrayList();

		/// <summary>
		/// Holds the amount of client.connect's currently in progress. We don't want to many of these going
		/// at once or theres a massive wait before they connect.
		/// If static so limit is across all instances of SmtpClientQueue.
		/// </summary>
		private static int _ConnectionAttemptsInProgress = 0;
		/// <summary>
		/// Lock for changing _ConnectionAttemptsInProgress. Is static so same lock across all class instances.
		/// </summary>
		private static object _ConnectionAttemptsInProgressLock = new object();

		private const int MAX_SIMULTANEOUS_CLIENT_CONNECT_ATTEMPTS = 5;

		/// <summary>
		/// Create an SmtpClientQueue instance.
		/// </summary>
		public SmtpClientQueue() : base()
		{
			Task.Run(new Action(RunInUseCleaner));
		}

		/// <summary>
		/// Removes dead & orphaned connections from InUseConnections.
		/// </summary>
		private async void RunInUseCleaner()
		{
			try
			{
				await Task.Run(new Action(async delegate()
					{
						while (true) // Run forever
						{
							// Look the collection, so it doesn't change under us.
							lock (InUseConnections.SyncRoot)
							{
								ArrayList toRemove = new ArrayList();
								
								// Loop through all connections and check they still exist and are connected, if there not then we should remove them.
								for (int i = 0; i < InUseConnections.Count; i++)
								{
									if (InUseConnections[i] == null || !((SmtpOutboundClient)InUseConnections[i]).Connected)
										toRemove.Add(i);
								}

								// Remove dead connections.
								for (int z = toRemove.Count - 1; z >= 0; z--)
									InUseConnections.RemoveAt((int)toRemove[z]);
							}

							// Don't want to loop to often so wait 30 seconds before next iteration.
							await Task.Delay(30 * 1000);
						}
					}));
			}
			catch (Exception)
			{
				RunInUseCleaner();
			}
		}

		/// <summary>
		/// Attempt to create a new connection using the specified ip address and mx record.
		/// </summary>
		/// <returns>A connected outbound client or NULL</returns>
		public SmtpOutboundClient CreateNewConnection(MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			SmtpOutboundClient smtpClient = null;

			// Get the maximum connections to the destination.
			int maximumConnections = OutboundRules.OutboundRuleManager.GetMaxConnectionsToDestination(ipAddress, mxRecord);

			lock (this.SyncRoot)
			{
				// Get the currently active connections count.
				int currentConnections = this.InUseConnections.Count;

				lock (_ConnectionAttemptsInProgressLock)
				{
					// If the current connections count + current connection is less than
					// the maximum connections then we can create a new connection otherwise
					// we are maxed out so return null.
					if (maximumConnections <= (currentConnections + _ConnectionAttemptsInProgress))
						return null;

				
					// Limit the amount of connection attempts or experiance massive delays 30s+ for client.connect()
					if (_ConnectionAttemptsInProgress >= SmtpClientQueue.MAX_SIMULTANEOUS_CLIENT_CONNECT_ATTEMPTS)
					{
						Logging.Debug("Cannot attempt to create new connection.");
						return null;
					}

					Logging.Debug("Attempting to create new connection.");
					_ConnectionAttemptsInProgress++;
				}
			}
			
			// Do the actual creating and connecting of the client outside of the lock
			// so we don't block other threads.

			try
			{
				// Create the new client and make the connection
				smtpClient = new SmtpOutboundClient(ipAddress);
				smtpClient.Connect(mxRecord);
				this.InUseConnections.Add(smtpClient);
			}
			catch (Exception ex)
			{
				// If something went wrong clear the client so we don't return something odd.
				smtpClient = null;
				if (ex is SocketException)
					throw ex;
			}
			finally
			{
				// Reduce the current attempts as were done.
				_ConnectionAttemptsInProgress--;
			}

			// Return connected client or null.
			return smtpClient;
		}
	}

	/// <summary>
	/// Holds a collection of SMTP Client Queues grouped by destination MX Hostname.
	/// </summary>
	internal class SmtpClientMxRecords : ConcurrentDictionary<string, SmtpClientQueue> { }

	/// <summary>
	/// Holds a collection of SmtpClientMxRecords grouped by outbound IP Address (as strings)
	/// </summary>
	internal class OutboundConnections : ConcurrentDictionary<string, SmtpClientMxRecords> { }

	internal class SmtpClientPool 
	{
		
		/// <summary>
		/// Holds the free and active connections from this SMTP client to other SMTP servers.
		/// </summary>
		private static OutboundConnections _OutboundConnections = new OutboundConnections();

		/// <summary>
		/// Attempts to get a SmtpClient using the outbound IP address and the specified MX records collection.
		/// </summary>
		/// <param name="outboundEndpoint">The local outbound endpoint we wan't to use.</param>
		/// <param name="mxs">The MX records for the domain we wan't a client to connect to.</param>
		/// <param name="deferalAction">The action to be called if service is unavalible or we are unable to 
		/// connect to any of the MX's in the MX records.</param>
		/// <returns>SmtpOutboundClient or Null.</returns>
		public static SmtpOutboundClient Dequeue(MtaIpAddress.MtaIpAddress ipAddress, MXRecord[] mxs, Action<string> deferalAction)
		{
			SmtpClientMxRecords mxConnections = _OutboundConnections.GetOrAdd(ipAddress.IPAddress.ToString(), new SmtpClientMxRecords());
			SmtpOutboundClient smtpClient = null;

			// Loop through all the MX Records.
			for (int i = 0; i < mxs.Length; i++)
			{
				try
				{
					// To prevent us bombarding a server that is blocking us we will check the service not available manager
					// to see if we can send to this MX, Max 1 message/minute, if we can't we won't.
					// At the moment we stop to all MXs for a domain if one of them responds with service unavailable.
					// This could be improved to allow others to continue, we should however if blocked on all MX's with 
					// lowest preference  not move on to the others.
					if (ServiceNotAvailableManager.IsServiceUnavailable(ipAddress.IPAddress.ToString(), mxs[i].Host))
					{
						deferalAction("Service unavailable");
						return null;
					}

					SmtpClientQueue clientQueue = mxConnections.GetOrAdd(mxs[i].Host, new SmtpClientQueue());

					// Loop through the client queue and make sure we get one thats still connected.
					// They may have idled out while waiting.
					while (!clientQueue.IsEmpty)
					{
						if (clientQueue.TryDequeue(out smtpClient))
						{
							if (smtpClient.Connected)
							{
								clientQueue.InUseConnections.Add(smtpClient);
								return smtpClient;
							}
						}
					}

					// Nothing was in the queue or all queued items timed out.
					return clientQueue.CreateNewConnection(ipAddress, mxs[i]);					
				}
				catch (SocketException ex)
				{
					Logging.Warn("Failed to connect to " + mxs[i].Host, ex);

					// Failed to connect to MX
					if (i == (mxs.Length - 1))
					{
						// There are no more to test
						deferalAction("Connect failed");
						return null;
					}
					else // There are more MX records to use to attempt connections.
						continue;
				}
			}

			return null;
		}

		/// <summary>
		/// Enqueue the SmtpOutboundClient for use by another message.
		/// </summary>
		/// <param name="client">The client to queue.</param>
		public static void Enqueue(SmtpOutboundClient client)
		{
			SmtpClientMxRecords mxConnections = _OutboundConnections.GetOrAdd(client.SmtpStream.LocalAddress.ToString(), new SmtpClientMxRecords());
			SmtpClientQueue clientQueue = mxConnections.GetOrAdd(client.MXRecord.Host, new SmtpClientQueue());
			clientQueue.Enqueue(client);
			try
			{
				clientQueue.InUseConnections.Remove(client);
			}
			catch (Exception)
			{
				// Already removed.
			}
		}
	}
}
