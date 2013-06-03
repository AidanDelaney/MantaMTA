using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Sockets;
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
			Start();
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
			MailAddress rcptTo = new MailAddress(msg.RcptTo);
			MailAddress mailFrom = new MailAddress(msg.MailFrom);
			//DNS.MXRecord[] mxs = DNS.DNSManager.GetMXRecords(rcptTo.Host);
			DNS.MXRecord[] mxs = new DNS.MXRecord[] { new DNS.MXRecord("RA", 1, uint.MaxValue) };
			if (mxs == null)
			{
				Console.WriteLine("Domain doesn't have MX");
				return;
			}

			
			using (TcpClient tcpClient = new TcpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(msg.OutboundIP), 0)))
			{
				for (int i = 0; i < mxs.Length; i++)
				{
					try
					{
						tcpClient.Connect(mxs[i].Host, 25);
					}
					catch(SocketException)
					{
						// Failed to connect to mx

						continue;
					}
						

					// We have connected to the MX, Say HELLO
					SmtpStreamHandler smtpStream = new SmtpStreamHandler(tcpClient);
					smtpStream.WriteLine("HELO " + System.Net.Dns.GetHostEntry(msg.OutboundIP).HostName);
					string response = smtpStream.ReadLine();
					smtpStream.WriteLine("MAIL FROM: <" + mailFrom.Address + ">");
					response = smtpStream.ReadLine();
					smtpStream.WriteLine("RCPT TO: <" + rcptTo.Address + ">");
					response = smtpStream.ReadLine();
					smtpStream.WriteLine("DATA");
					response = smtpStream.ReadLine();
					string[] dataLines = msg.Data.Split(Environment.NewLine.ToCharArray());
					for (int l = 0; l < dataLines.Length; l++)
					{
						smtpStream.WriteLine(dataLines[l], false);
					}
					smtpStream.WriteLine(".", false);
					response = smtpStream.ReadLine();
					smtpStream.WriteLine("QUIT");
					break;
				}
			}
		}
	}
}
