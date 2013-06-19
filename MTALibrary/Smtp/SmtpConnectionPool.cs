using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.DNS;

namespace Colony101.MTA.Library.Smtp
{
	/// <summary>
	/// Is a Thread safe queue used to hold connected and unused SmtpOutboundClients.
	/// </summary>
	internal class SmtpClientQueue : ConcurrentQueue<SmtpOutboundClient> { }

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
		/// <param name="smtpClient">Will be the SmtpClient of NULL.</param>
		/// <returns>True if smtpClient was set, false if it is NULL.</returns>
		public static bool TryDequeue(IPEndPoint outboundEndpoint, MXRecord[] mxs, Action<string> deferalAction, out SmtpOutboundClient smtpClient)
		{
			SmtpClientMxRecords mxConnections = _OutboundConnections.GetOrAdd(outboundEndpoint.Address.ToString(), new SmtpClientMxRecords());
			smtpClient = null;

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
					if (ServiceNotAvailableManager.IsServiceUnavailable(outboundEndpoint.Address.ToString(), mxs[i].Host))
					{
						deferalAction("Service unavailable");
						smtpClient = null;
						return false;
					}

					SmtpClientQueue clientQueue = mxConnections.GetOrAdd(mxs[i].Host, new SmtpClientQueue());

					// Loop through the client queue and make sure we get one thats still connected.
					// They may have idled out while waiting.
					while (!clientQueue.IsEmpty)
					{
						if (clientQueue.TryDequeue(out smtpClient))
						{
							if (smtpClient.Connected)
								return true;
						}
					}

					// Nothing was in the queue or all queued items timed out.
					// Create a new connection.
					smtpClient = new SmtpOutboundClient(outboundEndpoint);
					smtpClient.Connect(mxs[i]);
				}
				catch (SocketException ex)
				{
					Logging.Warn("Failed to connect to " + mxs[i].Host, ex);

					// Failed to connect to MX
					if (i == (mxs.Length - 1))
					{
						// There are no more to test
						deferalAction("Connect failed");
						smtpClient = null;
						return false;
					}
					else // There are more MX records to use to attempt connections.
						continue;
				}
			}

			return true;
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
		}
	}
}
