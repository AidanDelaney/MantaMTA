using MantaMTA.Core.Client.BO;
using MantaMTA.Core.DAL;
using MantaMTA.Core.DNS;
using MantaMTA.Core.Enums;
using MantaMTA.Core.RabbitMq;
using MantaMTA.Core.Sends;
using MantaMTA.Core.Smtp;
using MantaMTA.Core.VirtualMta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace MantaMTA.Core.Client
{
	/// <summary>
	/// MessageSender sends Emails to other servers from the Queue.
	/// </summary>
	public class MessageSender : IStopRequired
	{
		/// <summary>
		/// List of MX domains that we should not attempt to deliver to. The emails will hard bounce as "Domain Blacklisted".
		/// Todo: Put this in database and web interface.
		/// </summary>
		private List<string> _blacklistMx = new List<string> { 
			"uk-com-wildcard-null-mx.centralnic.net" 
		};

		#region Singleton
		/// <summary>
		/// The Single instance of this class.
		/// </summary>
		private static MessageSender _Instance = new MessageSender();
		
		/// <summary>
		/// Instance of the MessageSender class.
		/// </summary>
		public static MessageSender Instance
		{
			get
			{
				return MessageSender._Instance;
			}
		}

		private MessageSender()
		{
			MantaCoreEvents.RegisterStopRequiredInstance(this);
		}
		#endregion

		/// <summary>
		/// Holds the maximum amount of Tasks used for sending that should be run at anyone time.
		/// </summary>
		private int _MaxSendingWorkerTasks = -1;
		
		/// <summary>
		/// Holds the maximum amount of Tasks used for sending that should be run at anyone time.
		/// </summary>
		private int MAX_SENDING_WORKER_TASKS
		{
			get
			{
				if(_MaxSendingWorkerTasks == -1)
				{
					if (!int.TryParse(ConfigurationManager.AppSettings["MantaMaximumClientWorkers"], out _MaxSendingWorkerTasks))
					{
						Logging.Fatal("MantaMaximumClientWorkers not set in AppConfig");
						Environment.Exit(-1);
					}
					else if(_MaxSendingWorkerTasks < 1)
					{
						Logging.Fatal("MantaMaximumClientWorkers must be greater than 0");
						Environment.Exit(-1);
					}
					else
					{
						Logging.Info("Maximum Client Workers is " + _MaxSendingWorkerTasks.ToString());
					}
				}

				return _MaxSendingWorkerTasks;
			}
		}

		/// <summary>
		/// If TRUE then request for client to stop has been made.
		/// </summary>
		private bool _IsStopping = false;

		/// <summary>
		/// IStopRequired method. Will be called by MantaCoreEvents on stopping of MTA.
		/// </summary>
		public void Stop()
		{
			this._IsStopping = true;
		}


		public void Start()
		{
			Thread t = new Thread(new ThreadStart(() => {
				// Dictionary will hold a single int for each running task. The int means nothing.
				ConcurrentDictionary<Guid, int> runningTasks = new ConcurrentDictionary<Guid, int>();

				Action<MtaQueuedMessage> taskWorker = (qMsg) => {
					// Generate a unique ID for this task.
					Guid taskID = Guid.NewGuid();
					
					// Add this task to the running list.
					if (!runningTasks.TryAdd(taskID, 1))
						return;

					Task.Run(async () =>
					{
						try
						{
							// Loop while there is a task message to send.
							while (qMsg != null && !_IsStopping)
							{
								// Send the message.
								await SendMessageAsync(qMsg);

								if(!qMsg.IsHandled)
								{
									Logging.Warn("Message not handled " + qMsg.ID);
									qMsg.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
									RabbitMq.RabbitMqOutboundQueueManager.Enqueue(qMsg);
								}

								// Acknowledge of the message.
								RabbitMqOutboundQueueManager.Ack(qMsg);

								// Try to get another message to send.
								qMsg = RabbitMq.RabbitMqOutboundQueueManager.Dequeue();
							}
						}
						catch (Exception ex)
						{
							// Log if we can't send the message.
							Logging.Debug("Failed to send message", ex);
						}
						finally
						{
							// If there is still a acknowledge of the message.
							if (qMsg != null)
							{
								if (!qMsg.IsHandled)
								{
									Logging.Warn("Message not handled " + qMsg.ID);
									qMsg.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
									RabbitMq.RabbitMqOutboundQueueManager.Enqueue(qMsg);
								}

								RabbitMqOutboundQueueManager.Ack(qMsg);
							}

							// Remove this task from the dictionary
							int value;
							runningTasks.TryRemove(taskID, out value);
						}
					});
				};

				Action startWorkerTasks = () => {
					while ((runningTasks.Count < MAX_SENDING_WORKER_TASKS) && !_IsStopping)
					{
						MtaQueuedMessage qmsg = RabbitMq.RabbitMqOutboundQueueManager.Dequeue();
						if (qmsg == null)
							break; // Nothing to do, so don't start anymore workers.

						taskWorker(qmsg);
					}
				};

				while(!_IsStopping)
				{
					if (runningTasks.Count >= MAX_SENDING_WORKER_TASKS)
					{
						Thread.Sleep(100);
						continue;
					}

					startWorkerTasks();
				}
			}));
			t.Start();
		}


		/// <summary>
		/// Checks to see if the MX record collection contains blacklisted domains/ips.
		/// </summary>
		/// <param name="mxRecords">Collection of MX records to check.</param>
		/// <returns>True if collection contains blacklisted record.</returns>
		private bool IsMxBlacklisted(MXRecord[] mxRecords)
		{
			// Check for blacklisted MX
			foreach (var mx in mxRecords)
			{
				if (_blacklistMx.Contains(mx.Host.ToLower()))
					return true;
			}

			return false;
		}

		private async Task<bool> SendMessageAsync(MtaQueuedMessage msg)
		{
			// Check that the message next attempt after has passed.
			if (msg.AttemptSendAfterUtc > DateTime.UtcNow)
			{
				RabbitMqOutboundQueueManager.Enqueue(msg);
				await Task.Delay(50); // To prevent a tight loop within a Task thread we should sleep here.
				return false;
			}

			if (await MtaTransaction.HasBeenHandledAsync(msg.ID))
			{
				msg.IsHandled = true;
				return true;
			}

			// Get the send that this message belongs to so that we can check the send state.
			Send snd = SendManager.Instance.GetSend(msg.InternalSendID);

			switch(snd.SendStatus)
			{
				// The send is being discarded so we should discard the message.
				case SendStatus.Discard:
					await msg.HandleMessageDiscardAsync();
					return false;
				// The send is paused, the handle pause state will delay, without deferring, the message for a while so we can move on to other messages.
				case SendStatus.Paused:
					msg.HandleSendPaused();
					return false;
				// Send is active so we don't need to do anything.
				case SendStatus.Active:
					break;
				// Unknown send state, requeue the message and log error. Cannot send!
				default:
					msg.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
					RabbitMqOutboundQueueManager.Enqueue(msg);
					Logging.Error("Failed to send message. Unknown SendStatus[" + snd.SendStatus + "]!");
					return false;
			}
			

			bool result;
			// Check the message hasn't timed out. If it has don't attempt to send it.
			// Need to do this here as there may be a massive backlog on the server
			// causing messages to be waiting for ages after there AttemptSendAfter
			// before picking up. The MAX_TIME_IN_QUEUE should always be enforced.
			if (msg.AttemptSendAfterUtc - msg.QueuedTimestampUtc > new TimeSpan(0, MtaParameters.MtaMaxTimeInQueue, 0))
			{
				await msg.HandleDeliveryFailAsync("Timed out in queue.", null, null);
				result = false;
			}
			else
			{
				MailAddress mailAddress = new MailAddress(msg.RcptTo[0]);
				MailAddress mailFrom = new MailAddress(msg.MailFrom);
				MXRecord[] mXRecords = DNSManager.GetMXRecords(mailAddress.Host);
				// If mxs is null then there are no MX records.
				if (mXRecords == null || mXRecords.Length < 1)
				{
					await msg.HandleDeliveryFailAsync("550 Domain Not Found.", null, null);
					result = false;
				}
				else if(IsMxBlacklisted(mXRecords))
				{
					await msg.HandleDeliveryFailAsync("550 Domain blacklisted.", null, mXRecords[0]);
					result = false;
				}
				else
				{
					

					// The IP group that will be used to send the queued message.
					VirtualMtaGroup virtualMtaGroup = VirtualMtaManager.GetVirtualMtaGroup(msg.VirtualMTAGroupID);
					VirtualMTA sndIpAddress = virtualMtaGroup.GetVirtualMtasForSending(mXRecords[0]);

					SmtpOutboundClientDequeueResponse dequeueResponse = await SmtpClientPool.Instance.DequeueAsync(sndIpAddress, mXRecords);
					switch (dequeueResponse.DequeueResult)
					{
						case SmtpOutboundClientDequeueAsyncResult.Success:
						case SmtpOutboundClientDequeueAsyncResult.NoMxRecords:
						case SmtpOutboundClientDequeueAsyncResult.FailedToAddToSmtpClientQueue:
						case SmtpOutboundClientDequeueAsyncResult.Unknown:
							break; // Don't need to do anything for these results.
						case SmtpOutboundClientDequeueAsyncResult.FailedToConnect:
							await msg.HandleFailedToConnectAsync(sndIpAddress, mXRecords[0]);
							break;
						case SmtpOutboundClientDequeueAsyncResult.ServiceUnavalible:
							await msg.HandleServiceUnavailableAsync(sndIpAddress);
							break;
						case SmtpOutboundClientDequeueAsyncResult.Throttled:
							await msg.HandleDeliveryThrottleAsync(sndIpAddress, mXRecords[0]);
							break;
						case SmtpOutboundClientDequeueAsyncResult.FailedMaxConnections:
							msg.AttemptSendAfterUtc = DateTime.UtcNow.AddSeconds(2);
							RabbitMqOutboundQueueManager.Enqueue(msg);
							break;
					}

					SmtpOutboundClient smtpClient = dequeueResponse.SmtpOutboundClient;

					// If no client was dequeued then we can't currently send.
					// This is most likely a max connection issue. Return false but don't
					// log any deferal or throttle.
					if (smtpClient == null)
					{
						result = false;
					}
					else
					{
						try
						{
							Action<string> failedCallback = delegate(string smtpResponse)
							{
								// If smtpRespose starts with 5 then perm error should cause fail
								if (smtpResponse.StartsWith("5"))
									msg.HandleDeliveryFailAsync(smtpResponse, sndIpAddress, smtpClient.MXRecord).Wait();
								else
								{
									// If the MX is actively denying use service access, SMTP code 421 then we should inform
									// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
									if (smtpResponse.StartsWith("421"))
									{
										ServiceNotAvailableManager.Add(smtpClient.SmtpStream.LocalAddress.ToString(), smtpClient.MXRecord.Host, DateTime.UtcNow);
										msg.HandleDeliveryDeferral(smtpResponse, sndIpAddress, smtpClient.MXRecord, true);
									}
									else
									{
										// Otherwise message is deferred
										msg.HandleDeliveryDeferral(smtpResponse, sndIpAddress, smtpClient.MXRecord, false);
									}
								}
								throw new SmtpTransactionFailedException();
							};
							// Run each SMTP command after the last.
							await smtpClient.ExecHeloOrRsetAsync(failedCallback);
							await smtpClient.ExecMailFromAsync(mailFrom, failedCallback);
							await smtpClient.ExecRcptToAsync(mailAddress, failedCallback);
							await smtpClient.ExecDataAsync(msg.Message, failedCallback);
							SmtpClientPool.Instance.Enqueue(smtpClient);
							await msg.HandleDeliverySuccessAsync(sndIpAddress, smtpClient.MXRecord);
							result = true;
						}
						catch (SmtpTransactionFailedException)
						{
							// Exception is thrown to exit transaction, logging of deferrals/failers already handled.
							result = false;
						}
						catch (Exception ex)
						{
							Logging.Error("MessageSender error.", ex);
							if (msg != null)
								msg.HandleDeliveryDeferral("Connection was established but ended abruptly.", sndIpAddress, smtpClient.MXRecord, false);
							result = false;
						}
						finally
						{
							if (smtpClient != null)
							{
								smtpClient.IsActive = false;
								smtpClient.LastActive = DateTime.UtcNow;
							}
						}
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Exception is used to halt SMTP transaction if the server responds with unexpected code.
		/// </summary>
		[Serializable]
		private class SmtpTransactionFailedException : Exception { }
	}

	
}
