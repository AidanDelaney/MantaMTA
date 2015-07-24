using MantaMTA.Core.Client;
using MantaMTA.Core.DNS;
using MantaMTA.Core.OutboundRules;
using MantaMTA.Core.VirtualMta;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

		private const int MAX_SIMULTANEOUS_CLIENT_CONNECT_ATTEMPTS = 50;

		/// <summary>
		/// Create an SmtpClientQueue instance.
		/// </summary>
		public SmtpClientQueue()
			: base()
		{
			Task.Run(new Action(RunInUseCleaner));
		}

		/// <summary>
		/// Removes dead & orphaned connections from InUseConnections.
		/// </summary>
		private async void RunInUseCleaner()
		{

			await Task.Run(new Action(async delegate()
				{
					try
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
								{
									if (InUseConnections.Count < z)
									{
										((SmtpOutboundClient)InUseConnections[z]).Client.Close();
										((SmtpOutboundClient)InUseConnections[z]).Dispose();
										InUseConnections.RemoveAt((int)toRemove[z]);
									}
								}
							}

							// Don't want to loop to often so wait 30 seconds before next iteration.
							await Task.Delay(30 * 1000);
						}
					}
					catch (Exception)
					{
						//Logging.Debug("SmtpClientQueue :: RunInUseCleaner", ex);
						RunInUseCleaner();
					}
				}));
		}

		/// <summary>
		/// Attempt to create a new connection using the specified ip address and mx record.
		/// </summary>
		/// <returns>A connected outbound client or NULL</returns>
		public async Task<CreateNewConnectionAsyncResult> CreateNewConnectionAsync(VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
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
						return new CreateNewConnectionAsyncResult(new MaxConnectionsException());


					// Limit the amount of connection attempts or experiance massive delays 30s+ for client.connect()
					if (_ConnectionAttemptsInProgress >= SmtpClientQueue.MAX_SIMULTANEOUS_CLIENT_CONNECT_ATTEMPTS)
					{
						//Logging.Debug("Cannot attempt to create new connection.");
						return new CreateNewConnectionAsyncResult(new MaxConnectionsException());
					}

					//Logging.Debug("Attempting to create new connection.");
					_ConnectionAttemptsInProgress++;
				}
			}

			// Do the actual creating and connecting of the client outside of the lock
			// so we don't block other threads.

			try
			{
				// Create the new client and make the connection
				smtpClient = new SmtpOutboundClient(ipAddress);
				await smtpClient.ConnectAsync(mxRecord);
				smtpClient.IsActive = true;
				this.InUseConnections.Add(smtpClient);
			}
			catch (Exception ex)
			{
				// If something went wrong clear the client so we don't return something odd.
				if (smtpClient != null)
				{
					smtpClient.Close();
					smtpClient.Dispose();
					smtpClient = null;
				}
				if (ex is SocketException)
					return new CreateNewConnectionAsyncResult(ex);

				if (ex is AggregateException && ex.InnerException is System.IO.IOException)
					return new CreateNewConnectionAsyncResult(ex.InnerException);
			}
			finally
			{
				// Reduce the current attempts as were done.
				_ConnectionAttemptsInProgress--;
				if (smtpClient != null)
					smtpClient.IsActive = false;
			}

			// Return connected client or null.
			return new CreateNewConnectionAsyncResult(smtpClient);
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
		private static SmtpClientPool _Instance = new SmtpClientPool();

		/// <summary>
		/// Holds the free and active connections from this SMTP client to other SMTP servers.
		/// </summary>
		private OutboundConnections _OutboundConnections = new OutboundConnections();
		private object _ClientPoolLock = new object();
		public static SmtpClientPool Instance
		{
			get
			{
				return SmtpClientPool._Instance;
			}
		}
		private SmtpClientPool()
		{
			new Thread(new ThreadStart(delegate
			{
				while (true)
				{
					try
					{
						IEnumerator<KeyValuePair<string, SmtpClientMxRecords>> enumMXs = this._OutboundConnections.GetEnumerator();
						while (enumMXs.MoveNext())
						{
							int count = 0;
							int removed = 0;
							KeyValuePair<string, SmtpClientMxRecords> current = enumMXs.Current;
							SmtpClientMxRecords mxRecords = current.Value;
							IEnumerator<KeyValuePair<string, SmtpClientQueue>> enumClients = mxRecords.GetEnumerator();
							while (enumClients.MoveNext())
							{
								count++;
								KeyValuePair<string, SmtpClientQueue> current2 = enumClients.Current;
								SmtpClientQueue clients = current2.Value;
								if (clients.Count == 0 && clients.InUseConnections.Count == 0)
								{
									ConcurrentDictionary<string, SmtpClientQueue> arg_8B_0 = mxRecords;
									current2 = enumClients.Current;
									if (arg_8B_0.TryRemove(current2.Key, out clients))
									{
										//string arg_AF_0 = "Removed empty SMTP Clients Queue for ";
										//current2 = enumClients.Current;
										//Logging.Debug(arg_AF_0 + current2.Key);
										removed++;
									}
									else
									{
										string arg_D6_0 = "Failed to remove empty SMTP Clients Queue for ";
										current2 = enumClients.Current;
										Logging.Debug(arg_D6_0 + current2.Key);
									}
								}
							}
							Logging.Debug(string.Concat(new string[]
							{
								"SmtpClientPool : Removed ",
								removed.ToString("N0"),
								" client queues. ",
								(count - removed).ToString("N0"),
								" remaining."
							}));
						}
					}
					catch (Exception ex)
					{
						Logging.Debug("SmtpClientPool: " + ex);
					}
					finally
					{
						Thread.Sleep(60000);
					}
				}
			}))
			{
				IsBackground = true
			}.Start();
		}

		/// <summary>
		/// Attempts to get a SmtpClient using the outbound IP address and the specified MX records collection.
		/// 
		/// WARNING: returned SmtpOutboundClient will have it's IsActive flag set to true make sure to set it to
		///		     false when done with it or it will never be removed by the idle timeout.
		/// </summary>
		/// <param name="ipAddress">The local outbound endpoint we wan't to use.</param>
		/// <param name="mxs">The MX records for the domain we wan't a client to connect to.</param>
		/// <returns>Tuple containing a DequeueAsyncResult and either an SmtpOutboundClient or Null if failed to dequeue.</returns>
		public async Task<SmtpOutboundClientDequeueResponse> DequeueAsync(VirtualMTA ipAddress, MXRecord[] mxs)
		{
			// If there aren't any remote mx records then we can't send
			if (mxs.Length < 1)
				return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.NoMxRecords);

			SmtpClientMxRecords mxConnections = this._OutboundConnections.GetOrAdd(ipAddress.IPAddress.ToString(), new SmtpClientMxRecords());
			SmtpOutboundClient smtpClient = null;

			// Check that we aren't being throttled.
			if (!ThrottleManager.Instance.TryGetSendAuth(ipAddress, mxs[0]))
				return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.Throttled);

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
						return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.ServiceUnavalible);

					SmtpClientQueue clientQueue = null;
					lock (this._ClientPoolLock)
					{
						if (!mxConnections.TryGetValue(mxs[i].Host, out clientQueue))
						{
							clientQueue = new SmtpClientQueue();
							if (!mxConnections.TryAdd(mxs[i].Host, clientQueue))
								return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.FailedToAddToSmtpClientQueue);
						}
					}
					// Loop through the client queue and make sure we get one thats still connected.
					// They may have idled out while waiting.
					while (!clientQueue.IsEmpty)
					{
						if (clientQueue.TryDequeue(out smtpClient))
						{
							if (smtpClient.Connected)
							{
								clientQueue.InUseConnections.Add(smtpClient);
								smtpClient.LastActive = DateTime.UtcNow;
								smtpClient.IsActive = true;
								return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.Success, smtpClient);
							}
						}
					}

					// Nothing was in the queue or all queued items timed out.
					CreateNewConnectionAsyncResult createNewConnectionResult = await clientQueue.CreateNewConnectionAsync(ipAddress, mxs[i]);
					if (createNewConnectionResult.Exception == null)
						return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.Success, createNewConnectionResult.OutboundClient);
					else if(createNewConnectionResult.Exception is MaxConnectionsException)
						return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.FailedMaxConnections);
					else
						throw createNewConnectionResult.Exception;
				}
				catch (SocketException ex)
				{
					// We have failed to connect to the remote host.
					Logging.Warn("Failed to connect to " + mxs[i].Host, ex);

					// If we fail to connect to an MX then don't try again for at least a minute.
					ServiceNotAvailableManager.Add(ipAddress.IPAddress.ToString(), mxs[i].Host, DateTime.UtcNow);

					// Failed to connect to MX
					if (i == mxs.Length - 1)
						return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.FailedToConnect);
				}
				catch(Exception ex)
				{
					// Something unexpected and unhandled has happened, log it and return unknown.
					Logging.Error("SmtpClientPool.DequeueAsync Unhandled Exception", ex);
					return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.Unknown);
				}
			}

			// It we got here then we have failed to connect to the remote host.
			return new SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult.FailedToConnect);
		}

		/// <summary>
		/// Enqueue the SmtpOutboundClient for use by another message.
		/// </summary>
		/// <param name="client">The client to queue.</param>
		public void Enqueue(SmtpOutboundClient client)
		{
			SmtpClientMxRecords mxConnections = this._OutboundConnections.GetOrAdd(client.SmtpStream.LocalAddress.ToString(), new SmtpClientMxRecords());
			SmtpClientQueue clientQueue = null;
			lock (this._ClientPoolLock)
			{
				if (!mxConnections.TryGetValue(client.MXRecord.Host, out clientQueue))
				{
					clientQueue = new SmtpClientQueue();
					if (!mxConnections.TryAdd(client.MXRecord.Host, clientQueue))
					{
						throw new Exception("Failed to add new SmtpClientQueue");
					}
				}
			}
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

	/// <summary>
	/// Represents a dequeue request response from the SmtpClientPool.DequeueAsync method.
	/// </summary>
	internal class SmtpOutboundClientDequeueResponse : Tuple<SmtpOutboundClientDequeueAsyncResult, SmtpOutboundClient>
	{
		/// <summary>
		/// The OutboundClient that was dequeued or created, will be null if one wasn't dequeued.
		/// </summary>
		public SmtpOutboundClient SmtpOutboundClient { get { return base.Item2; } }

		/// <summary>
		/// The result of the dequeue result.
		/// </summary>
		public SmtpOutboundClientDequeueAsyncResult DequeueResult { get { return base.Item1; } }

		/// <summary>
		/// Creates a reponse for a call to the DequeueAsync method.
		/// </summary>
		/// <param name="dequeueResult">The result of the method call.</param>
		/// <param name="client">The SmtpOutboundClient if one was dequeued, should be null if not.</param>
		public SmtpOutboundClientDequeueResponse(SmtpOutboundClientDequeueAsyncResult dequeueResult, SmtpOutboundClient client = null)
			: base(dequeueResult, client)
		{

		}
	}

	/// <summary>
	/// Represents the result of a call to the SmtpClientPool.DequeueAsync method.
	/// </summary>
	internal enum SmtpOutboundClientDequeueAsyncResult
	{
		/// <summary>
		/// The dequeue call resulted in an Unknown state.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The dequeue was successful.
		/// </summary>
		Success = 1,
		/// <summary>
		/// The MX Records collection passed in the method call was empty.
		/// </summary>
		NoMxRecords = 2,
		/// <summary>
		/// A client has not been dequeued as throttling is in effect.
		/// </summary>
		Throttled = 3,
		/// <summary>
		/// A client to the requested host was not attempted as the service is currently unavalible.
		/// </summary>
		ServiceUnavalible = 4,
		/// <summary>
		/// This should never happen, it means that a client exists but we couldn't add it to the client tracking library,
		/// as such it has been disconnected and disposed.
		/// </summary>
		FailedToAddToSmtpClientQueue = 5,
		/// <summary>
		/// A client was created but it was unable to connect to the remote host.
		/// </summary>
		FailedToConnect = 6,
		/// <summary>
		/// No attempt was made to dequeue a client as the host has already hit it's maximum connections.
		/// </summary>
		FailedMaxConnections = 7
	}

	/// <summary>
	/// Represents a result from the SmtpClientPool.CreateNewConnectionAsync method.
	/// </summary>
	internal class CreateNewConnectionAsyncResult : Tuple<Exception, SmtpOutboundClient>
	{
		/// <summary>
		/// Represents an Exception that happened within the CreateNewConnectionAsync method. 
		/// Will be null if none occurred.
		/// </summary>
		public Exception Exception { get { return base.Item1; } }

		/// <summary>
		/// The SmtpOutboundClient that was created and connected to the remote host.
		/// Will be null if an exception occurred.
		/// </summary>
		public SmtpOutboundClient OutboundClient { get { return base.Item2; } }

		/// <summary>
		/// Creates a new CreateNewConnectionAsyncResult for when the CreateNewConnectionAsync methoid resulted in an exception.
		/// </summary>
		/// <param name="exception">The Exception.</param>
		public CreateNewConnectionAsyncResult(Exception exception) : base(exception, null) { }

		/// <summary>
		/// Creates a new CreateNewConnectionAsyncResult for when the CreateNewConnectionAsync methoid succeded and created and connected an outbound connection.
		/// </summary>
		/// <param name="outboundClient">The SmtpOutboundClient.</param>
		public CreateNewConnectionAsyncResult(SmtpOutboundClient outboundClient) : base(null, outboundClient) { }
	}
}
