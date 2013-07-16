using System;
using System.Collections;
using System.Collections.Concurrent;
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
		/// Holds the amount of messages that should be picked up as a single batch
		/// for sending. Batches are used so we don't need to pickup all messages
		/// in one go.
		/// </summary>
		private const int _PICKUP_SIZE = 100;
		
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
		public static void Enqueue(Guid messageID, int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			MtaMessage msg = MtaMessage.Create(messageID, internalSendID, mailFrom, rcptTo);
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
						// Get the first block of messages
						MtaQueuedMessageCollection messagesToSend = DAL.MtaMessageDB.PickupForSending(_PICKUP_SIZE);

						while (!_IsStopping) // Run until stop requested
						{
							// Dictionary will hold a single int for each running task. The int means nothing.
							ConcurrentDictionary<Guid, int> runningTasks = new ConcurrentDictionary<Guid, int>();
							
							// Loop through all of the messages in this batch and run the Send task
							for (int i = 0; i < messagesToSend.Count; i++)
							{
								// Get the message for this loop.
								MtaQueuedMessage queuedMessage = messagesToSend[i];

								// Don't try and send the message if stop has been issued.
								if (!_IsStopping)
								{
									// Generate a unique ID for this task.
									Guid taskID = Guid.NewGuid();

									// Add this task to the running list.
									if (!runningTasks.TryAdd(taskID, 1))
										return;


									Task.Run(new Action(async delegate()
									{
										try
										{
											// Send the message
											await SendMessageAsync(queuedMessage);
										}
										catch (Exception ex)
										{
											// Log if we can't send the message.
											Logging.Debug("Failed to send message", ex);
										}
										finally
										{
											// Remove this task from the dictionary
											int value;
											runningTasks.TryRemove(taskID, out value);
										}
									})).ContinueWith(task => queuedMessage.Dispose()); // Always dispose of the queued message.
								}
								else // Stop requested, dispose the message without any attempt to send it.
									queuedMessage.Dispose();
							}

							// As long as tasks are running then we should wait here.
							while (runningTasks.Count > 0)
							{
								System.Threading.Thread.Sleep(10);
							}

							// If not stopping get another batch of messages.
							if (!_IsStopping)
							{
								messagesToSend = DAL.MtaMessageDB.PickupForSending(_PICKUP_SIZE);
								
								// There are no more messages at the moment. Take a nap so not to hammer cpu.
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

		/// <summary>
		/// Sends the specified message.
		/// </summary>
		/// <param name="msg">Message to send</param>
		/// <returns>True if message sent, false if not.</returns>
		private static async Task<bool> SendMessageAsync(MtaQueuedMessage msg)
		{
			// Check the message hasn't timed out. If it has don't attempt to send it.
			// Need to do this here as there may be a massive backlog on the server
			// causing messages to be waiting for ages after there AttemptSendAfter
			// before picking up. The MAX_TIME_IN_QUEUE should always be enforced.
			if ((msg.AttemptSendAfterUtc - msg.QueuedTimestampUtc) > new TimeSpan(0, MtaParameters.MtaMaxTimeInQueue, 0))
			{
				msg.HandleDeliveryFail("Timed out in queue.", null, null);
				return false;
			}

			MailAddress rcptTo = msg.RcptTo[0];
			MailAddress mailFrom = msg.MailFrom;

			
			DNS.MXRecord[] mxs = DNS.DNSManager.GetMXRecords(rcptTo.Host);
			// If mxs is null then there are no MX records.
			if (mxs == null)
			{
				msg.HandleDeliveryFail("Domain doesn't exist.", null, null);
				return false;
			}

			// The IP group that will be used to send the queued message.
			MtaIpAddress.MtaIPGroup messageIpGroup = MtaIpAddress.IpAddressManager.GetMtaIPGroup(msg.IPGroupID);
			MtaIpAddress.MtaIpAddress sndIpAddress = messageIpGroup.GetIpAddressForSending(mxs[0]);

			ArrayList noneThrottledMXs = new ArrayList();
			for (int i = 0; i < mxs.Length; i++)
			{
				// Check not throttled
				if (OutboundRules.ThrottleManager.Instance.TryGetSendAuth(sndIpAddress, mxs[i]))
					noneThrottledMXs.Add(mxs[i]);
			}

			// If there are no MXs to send to then we are currently being throttled.
			if (noneThrottledMXs.Count == 0)
			{
				msg.HandleDeliveryThrottle(sndIpAddress, null);
				return false;
			}


			SmtpOutboundClient smtpClient = SmtpClientPool.Dequeue(sndIpAddress, (DNS.MXRecord[])noneThrottledMXs.ToArray(typeof(DNS.MXRecord)), 
				delegate(string message)
				{
					msg.HandleDeliveryDeferral(message, sndIpAddress, null);
				});

			// If no client was dequeued then we can't currently send.
			// This is most likely a max connection issue. Return false but don't
			// log any deferal or throttle.
			if (smtpClient == null)
				return false;

			try
			{
				Action<string> handleSmtpError = new Action<string>(delegate(string smtpResponse)
				{
					// If smtpRespose starts with 5 then perm error should cause fail
					if (smtpResponse.StartsWith("5"))
						msg.HandleDeliveryFail(smtpResponse, sndIpAddress, smtpClient.MXRecord);
					else
					{
						// If the MX is actively denying use service access, SMTP code 421 then we should inform
						// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
						if (smtpResponse.StartsWith("421"))
							ServiceNotAvailableManager.Add(smtpClient.SmtpStream.LocalAddress.ToString(), smtpClient.MXRecord.Host, DateTime.UtcNow);

						// Otherwise message is deferred
						msg.HandleDeliveryDeferral(smtpResponse, sndIpAddress, smtpClient.MXRecord);
					}

					throw new SmtpTransactionFailedException();
				});

				// Run each SMTP command after the last.
				await smtpClient.ExecHeloOrRsetAsync(handleSmtpError);
				await smtpClient.ExecMailFromAsync(mailFrom, handleSmtpError);
				await smtpClient.ExecRcptToAsync(rcptTo, handleSmtpError);
				await smtpClient.ExecDataAsync(msg.Data, handleSmtpError);
				SmtpClientPool.Enqueue(smtpClient);
				msg.HandleDeliverySuccess(sndIpAddress, smtpClient.MXRecord);
					
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

		/// <summary>
		/// Exception is used to halt SMTP transaction if the server responds with unexpected code.
		/// </summary>
		[Serializable]
		private class SmtpTransactionFailedException : Exception { }
	}

	
}
