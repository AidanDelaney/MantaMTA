using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading;
using Colony101.MTA.Library.DAL;

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
						List<MtaQueueItem> messagesToSend = DAL.MtaQueueDB.PickupAndLockQueueItems(10);
						while (messagesToSend.Count > 0)
						{
							for (int i = 0; i < messagesToSend.Count; i++)
							{
								SendMessage(messagesToSend[i]);
							}

							messagesToSend.RemoveAll(m => true);
						}
					}));
				_ClientThread.Start();
			}
		}

		private static void SendMessage(MtaQueueItem msg)
		{
			Action<string> doLookup = new Action<string>(delegate(string domain)
				{
					Console.WriteLine("DNS Query for " + domain);
					System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
					sw.Start();
					try
					{
						DNS.MXRecord[] results = DNS.DNSManager.GetMXRecords(domain);

						if (results == null)
							Console.WriteLine("Domain doesn't have MX");
						//else
						//	foreach (DNS.MXRecord mx in results)
						//		Console.WriteLine(mx.Priority + " " + mx.Host + " " + mx.Dead);
					}
					catch (DNS.DNSDomainNotFoundException)
					{
						Console.WriteLine("Domain doesn't exist in DNS.");
					}
					sw.Stop();
					Console.WriteLine(sw.Elapsed.ToString());
				});

			MailAddress rcptTo = new MailAddress(msg.RcptTo);
			doLookup(rcptTo.Host);
			doLookup(rcptTo.Host);
			doLookup(rcptTo.Host);
			doLookup(rcptTo.Host);
			doLookup(rcptTo.Host);
		}
	}
}
