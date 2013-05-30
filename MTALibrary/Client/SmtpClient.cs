using System;
using System.Threading;

namespace Colony101.MTA.Library.Client
{
	public class SmtpClient
	{
		private static Thread _ClientThread = null;

		/// <summary>
		/// Enqueue a message for delivery.
		/// </summary>
		/// <param name="outboundIP">The IP address that should be used to relay this message.</param>
		/// <param name="mailFrom"></param>
		/// <param name="rcptTo"></param>
		/// <param name="message"></param>
		public static void Enqueue(string outboundIP, string mailFrom, string[] rcptTo, string message)
		{
			DAL.MtaQueueDB.Insert(outboundIP, Guid.NewGuid(), mailFrom, string.Join(",", rcptTo), message);
		}

		public static void Start()
		{
			if (_ClientThread == null || _ClientThread.ThreadState != ThreadState.Running)
			{
				_ClientThread = new Thread(new ThreadStart(delegate()
					{
						 DAL.MtaQueueDB.PickupAndLockQueueItems(10);

					}));
				_ClientThread.Start();
			}
		}
	}
}
