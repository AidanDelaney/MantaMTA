using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.DNS;

namespace Colony101.MTA.Library.Smtp
{
	internal class SmtpClientQueue : ConcurrentQueue<SmtpOutboundClient>
	{

	}

	internal class SmtpClientMxRecords : ConcurrentDictionary<MXRecord, SmtpClientQueue>
	{

	}

	internal class OutboundConnections : ConcurrentDictionary<IPAddress, SmtpClientMxRecords>
	{

	}

	internal class SmtpClientPool 
	{
		private static OutboundConnections _OutboundConnections = new OutboundConnections();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="outboundEndpoint"></param>
		/// <param name="mxs"></param>
		/// <param name="deferalDelegate"></param>
		/// <param name="smtpClient"></param>
		/// <returns></returns>
		public static bool TryDequeue(IPEndPoint outboundEndpoint, MXRecord[] mxs, Action<string> deferalDelegate, out SmtpOutboundClient smtpClient)
		{
			SmtpClientMxRecords mxConnections = _OutboundConnections.GetOrAdd(outboundEndpoint.Address, new SmtpClientMxRecords());
			smtpClient = null;

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
						deferalDelegate("Service unavailable");
						smtpClient = null;
						return false;
					}

					SmtpClientQueue clientQueue = mxConnections.GetOrAdd(mxs[i], new SmtpClientQueue());

					while (!clientQueue.IsEmpty)
					{
						if (clientQueue.TryDequeue(out smtpClient))
						{
							if (smtpClient.Connected)
								return true;
						}
					}


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
						deferalDelegate("Connect failed");
						smtpClient = null;
						return false;
					}
					else
						continue;
				}
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		public static void Enqueue(SmtpOutboundClient client)
		{
			SmtpClientMxRecords mxConnections = _OutboundConnections.GetOrAdd(client.SmtpStream.LocalAddress, new SmtpClientMxRecords());
			SmtpClientQueue clientQueue = mxConnections.GetOrAdd(client.MXRecord, new SmtpClientQueue());
			clientQueue.Enqueue(client);
		}
	}
}
