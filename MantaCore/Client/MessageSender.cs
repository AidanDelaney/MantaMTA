using System;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MantaMTA.Core.Client.BO;
using MantaMTA.Core.Smtp;

namespace MantaMTA.Core.Client
{
	/// <summary>
	/// MessageSender sends Emails to other servers from the Queue.
	/// </summary>
	public static class MessageSender
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
		public static void Enqueue(int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			MtaMessage msg = MtaMessage.Create(internalSendID, mailFrom, rcptTo);
			msg.Queue(message, ipGroupID);
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
							Parallel.For(0, messagesToSend.Count, new Action<int>(delegate(int i)
							{
								// Don't try and send the message if stop has been issued.
								if (!_IsStopping)
								{
									Task.Run(() => SendMessage(messagesToSend[i])).Wait();
									messagesToSend[i].Dispose();
								}
							}));

							// If not stopping get another batch of messages.
							if (!_IsStopping)
							{
								messagesToSend = DAL.MtaMessageDB.PickupForSending(10);
								if (messagesToSend.Count == 0)
									Thread.Sleep(15 * 1000);
							}
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

		private static async Task<bool> SendMessage(MtaQueuedMessage msg)
		{
			// Check the message hasn't timed out. If it has don't attempt to send it.
			// Need to do this here as there may be a massive backlog on the server
			// causing messages to be waiting for ages after there AttemptSendAfter
			// before picking up. The MAX_TIME_IN_QUEUE should always be enforced.
			if ((msg.AttemptSendAfter - msg.QueuedTimestamp) > new TimeSpan(0, MtaParameters.MtaMaxTimeInQueue, 0))
			{
				msg.HandleDeliveryFail("Timed out in queue.");
				return false;
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
				return false;
			}

			// The IP group that will be used to send the queued message.
			MtaIpAddress.MtaIPGroup messageIpGroup = MtaIpAddress.IpAddressManager.GetMtaIPGroup(msg.IPGroupID);
			MtaIpAddress.MtaIpAddress sndIpAddress = messageIpGroup.GetRandomIP();

			ArrayList noneThrottledMXs = new ArrayList();
			for (int i = 0; i < mxs.Length; i++)
			{
				// Check not throttled
				if (OutboundRules.ThrottleManager.Instance.TryGetSendAuth(sndIpAddress, mxs[i]))
					noneThrottledMXs.Add(mxs[i]);
			}
			if (noneThrottledMXs.Count == 0)
			{
				msg.HandleDeliveryThrottle();
				return false;
			}


			MantaMTA.Core.Smtp.SmtpOutboundClient smtpClient = null;
			if (SmtpClientPool.TryDequeue(sndIpAddress, (DNS.MXRecord[])noneThrottledMXs.ToArray(typeof(DNS.MXRecord)), 
				delegate(string message)
				{
					msg.HandleDeliveryDeferral(message);
				}, out smtpClient))
			{
				try
				{
					Action<string> handleSmtpError = new Action<string>(delegate(string smtpResponse)
					{
						// If smtpRespose starts with 5 then perm error should cause fail
						if (smtpResponse.StartsWith("5"))
							msg.HandleDeliveryFail(smtpResponse);
						else
						{
							// If the MX is actively denying use service access, SMTP code 421 then we should inform
							// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
							if (smtpResponse.StartsWith("421"))
								ServiceNotAvailableManager.Add(smtpClient.SmtpStream.LocalAddress.ToString(), smtpClient.MXRecord.Host, DateTime.Now);

							// Otherwise message is deferred
							msg.HandleDeliveryDeferral(smtpResponse);
						}

						throw new SmtpTransactionFailedException();
					});

					// Run each SMTP command after the last.
					await Task.Run(() => smtpClient.ExecHeloOrRset(handleSmtpError))
						.ContinueWith(task => smtpClient.ExecMailFrom(mailFrom, handleSmtpError), TaskContinuationOptions.OnlyOnRanToCompletion)
						.ContinueWith(task => smtpClient.ExecRcptTo(rcptTo, handleSmtpError), TaskContinuationOptions.OnlyOnRanToCompletion)
						.ContinueWith(task => smtpClient.ExecData(msg.Data, handleSmtpError), TaskContinuationOptions.OnlyOnRanToCompletion)
						.ContinueWith(task => SmtpClientPool.Enqueue(smtpClient), TaskContinuationOptions.OnlyOnRanToCompletion)
						.ContinueWith(task => msg.HandleDeliverySuccess(), TaskContinuationOptions.OnlyOnRanToCompletion);
					
					return true;
				}
				catch (SmtpTransactionFailedException)
				{
					// Exception is thrown to exit transaction, logging of deferrals/failers already handled.
					return false;
				}
				catch (Exception)
				{
					return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Exception is used to halt SMTP transaction if the server responds with unexpected code.
		/// </summary>
		[Serializable]
		private class SmtpTransactionFailedException : Exception { }
	}

	
}
