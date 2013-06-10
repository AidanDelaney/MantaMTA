using System;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using Colony101.MTA.Library.Client.BO;
using Colony101.MTA.Library.Enums;

namespace Colony101.MTA.Library.Client
{
	/// <summary>
	/// SMTP Client sends Emails to other servers from the Queue.
	/// </summary>
	public static class SmtpClient
	{
		/// <summary>
		/// Client thread.
		/// </summary>
		private static Thread _ClientThread = null;
		/// <summary>
		/// If TRUE then request for client to stop has been made.
		/// </summary>
		private static bool _IsStopping = false;
		/// <summary>
		/// Enqueue a message for delivery.
		/// </summary>
		/// <param name="outboundIP">The IP address that should be used to relay this message.</param>
		/// <param name="mailFrom"></param>
		/// <param name="rcptTo"></param>
		/// <param name="message"></param>
		public static void Enqueue(string outboundIP, string mailFrom, string[] rcptTo, string message)
		{
			MtaMessage msg = MtaMessage.Create(outboundIP, mailFrom, rcptTo, message);
			msg.Queue();
		}

		/// <summary>
		/// Starts the SMTP Client.
		/// </summary>
		public static void Start()
		{
			if (_ClientThread == null || _ClientThread.ThreadState != ThreadState.Running)
			{
				_IsStopping = false;
				_ClientThread = new Thread(new ThreadStart(delegate()
					{
						MtaQueuedMessageCollection messagesToSend = DAL.MtaMessageDB.PickupForSending(10);
						while (!_IsStopping)
						{
							for (int i = 0; i < messagesToSend.Count; i++)
							{
								try
								{
									SendMessage(messagesToSend[i]);
								}
								finally
								{
									messagesToSend[i].Dispose();
								}
							}

							messagesToSend = DAL.MtaMessageDB.PickupForSending(10);
							if (messagesToSend.Count == 0)
								Thread.Sleep(15 * 1000);
						}
					}));
				_ClientThread.Start();
			}
		}

		/// <summary>
		/// Stop the client from sending.
		/// </summary>
		public static void Stop()
		{
			_IsStopping = true;
		}

		private static void SendMessage(MtaQueuedMessage msg)
		{
			// Check the message hasn't timed out. If it has don't attempt to send it.
			// Need to do this here as there may be a massive backlog on the server
			// causing messages to be waiting for ages after there AttemptSendAfter
			// before picking up. The MAX_TIME_IN_QUEUE should always be enforced.
			if ((msg.AttemptSendAfter - msg.QueuedTimestamp) > new TimeSpan(0, MtaParameters.MTA_MAX_TIME_IN_QUEUE, 0))
			{
				msg.HandleDeliveryFail("Timed out in queue.");
				return;
			}

			MailAddress rcptTo = msg.RcptTo[0];
			MailAddress mailFrom = msg.MailFrom;

			DNS.MXRecord[] mxs = null;
			try
			{
				mxs = DNS.DNSManager.GetMXRecords(rcptTo.Host);
			}
			catch (DNS.DNSDomainNotFoundException)
			{
				try
				{
					// If there are no MX records use the hostname.
					if(Dns.GetHostAddresses(rcptTo.Host).Length >0)
						mxs = new DNS.MXRecord[] { new DNS.MXRecord(rcptTo.Host, 10, uint.MaxValue) };
				}
				catch(Exception){}
			}
			
			// If mxs is null then there are no MX records.
			if (mxs == null)
			{
				msg.HandleDeliveryFail("Domain doesn't exist.");
				return;
			}

			using (TcpClient tcpClient = new TcpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(msg.OutboundIP), 0)))
			{
				string connectedMX = string.Empty;
				for (int i = 0; i < mxs.Length; i++)
				{
					try
					{
						// To prevent us bombarding a server that is blocking us we will check the service not available manager
						// to see if we can send to this MX, Max 1 message/minute, if we can't we won't.
						// At the moment we stop to all MXs for a domain if one of them responds with service unavailable.
						// This could be improved to allow others to continue, we should however if blocked on all MX's with 
						// lowest preference  not move on to the others.
						if (ServiceNotAvailableManager.IsServiceUnavailable(msg.OutboundIP, mxs[i].Host))
						{
							msg.HandleDeliveryDeferral("Service unavailable");
							return;
						}

						tcpClient.Connect(mxs[i].Host, 25);
						connectedMX = mxs[i].Host;
					}
					catch(SocketException)
					{
						// Failed to connect to MX
						if (i == (mxs.Length - 1))
						{
							// There are no more to test
							msg.HandleDeliveryDeferral("Connect failed");
							return;
						}
						else
							continue;
					}


					try
					{
						Action<SmtpStreamHandler, string> handleSmtpError = new Action<SmtpStreamHandler, string>(delegate(SmtpStreamHandler streamHandler, string smtpResponse)
						{
							// If smtpRespose starts with 5 then perm error should cause fail
							if (smtpResponse.StartsWith("5"))
								msg.HandleDeliveryFail(smtpResponse);
							else
							{
								// Otherwise message is deferred
								msg.HandleDeliveryDeferral(smtpResponse);

								// If the MX is actively denying use service access, SMTP code 421 then we should inform
								// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
								if (smtpResponse.StartsWith("421"))
									ServiceNotAvailableManager.Add(msg.OutboundIP, connectedMX, DateTime.Now);
							}
						});


						// Read the Server greeting.
						SmtpStreamHandler smtpStream = new SmtpStreamHandler(tcpClient);
						string response = smtpStream.ReadAllLines();
						if (!response.StartsWith("2"))
						{
							handleSmtpError(smtpStream, response);
							return;
						}

						// Action for sending commands to SMTP server.
						Action<string, string> doCommand = delegate(string cmd, string expectedResult)
						{
							smtpStream.WriteLine(cmd);
							response = smtpStream.ReadAllLines();
							if (!response.StartsWith(expectedResult))
							{
								handleSmtpError(smtpStream, response);
								throw new SmtpTransactionFailedException();
							}
						};

						SmtpTransportMIME transportMime = SmtpTransportMIME._8BitUTF;

						// We have connected to the MX, Say EHLO.
						smtpStream.WriteLine("EHLO " + System.Net.Dns.GetHostEntry(msg.OutboundIP).HostName);
						response = smtpStream.ReadAllLines();
						if (!response.StartsWith("2"))
							// If server didn't respond with a success code on hello then we should retry with HELO
							doCommand("HELO " + System.Net.Dns.GetHostEntry(msg.OutboundIP).HostName, "250");
						else
						{
							// Server responded to EHLO
							// Check to see if it supports 8BITMIME
							if (response.IndexOf("8BITMIME", StringComparison.OrdinalIgnoreCase) > -1)
								transportMime = SmtpTransportMIME._8BitUTF;
						}

						doCommand("MAIL FROM: <" + 
											(mailFrom == null ? string.Empty : mailFrom.Address) + ">" +
											(transportMime == SmtpTransportMIME._8BitUTF ? " BODY=8BITMIME" : string.Empty), "250");
						doCommand("RCPT TO: <" + rcptTo.Address + ">", "250");
						doCommand("DATA", "354");

						// Send the message data using the correct transport MIME
						smtpStream.SetSmtpTransportMIME(transportMime);
						smtpStream.Write(msg.Data, false);
						smtpStream.Write(Environment.NewLine + "." + Environment.NewLine, false);

						// Data done so return to 7-Bit mode.
						smtpStream.SetSmtpTransportMIME(SmtpTransportMIME._7BitASCII);
						response = smtpStream.ReadAllLines();
						if (!response.StartsWith("250"))
						{
							handleSmtpError(smtpStream, response);
							return;
						}

						smtpStream.WriteLine("QUIT");
						msg.HandleDeliverySuccess();
						return;
					}
					catch (SmtpTransactionFailedException)
					{
						// Exception is thrown to exit transaction, logging of deferrals/failers already handled.
						return;
					}
					catch (Exception)
					{
						return;
					}
				}
			}
		}

		/// <summary>
		/// Exception is used to halt SMTP transaction if the server responds with unexpected code.
		/// </summary>
		[Serializable]
		private class SmtpTransactionFailedException : Exception { }
	}

	
}
