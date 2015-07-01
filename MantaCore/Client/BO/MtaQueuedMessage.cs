using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;
using MantaMTA.Core.Events;
using MantaMTA.Core.RabbitMq;
using System;
using System.Threading.Tasks;

namespace MantaMTA.Core.Client.BO
{
	/// <summary>
	/// Holds a QUEUED MtaMesage
	/// </summary>
	internal class MtaQueuedMessage : MtaMessage
	{
		/// <summary>
		/// 
		/// </summary>
		public bool IsHandled { get; set; }

		/// <summary>
		/// Timestamp of when the message was originally queued.
		/// </summary>
		public DateTime QueuedTimestampUtc { get; set; }

		/// <summary>
		/// Timestamp of the earliest the first/next attempt to send the message should be made.
		/// </summary>
		public DateTime AttemptSendAfterUtc { get; set; }

		/// <summary>
		/// Number of times that this message has been queued.
		/// </summary>
		public int DeferredCount { get; set; }

		public MtaQueuedMessage()
		{
		}

		/// <summary>
		/// Create a new MtaOutboundMessage from the InboundMessage.
		/// </summary>
		/// <param name="inbound">Inbound message to create this outbound message from.</param>
		/// <returns>The outbound message.</returns>
		public static MtaQueuedMessage CreateNew(MtaMessage inbound)
		{
			MtaQueuedMessage outbound = new MtaQueuedMessage
			{
				DeferredCount = 0,
				InternalSendID = inbound.InternalSendID,
				MailFrom = inbound.MailFrom,
				Message = inbound.Message,
				ID = inbound.ID,
				AttemptSendAfterUtc = DateTime.UtcNow,
				QueuedTimestampUtc = DateTime.UtcNow,
				RcptTo = inbound.RcptTo,
				VirtualMTAGroupID = inbound.VirtualMTAGroupID,
				IsHandled = false
			};

			return outbound;
		}

		/// <summary>
		/// This method handles message deferal.
		///	Logs deferral
		///	Fails the message if timed out
		/// or
		/// Sets the next rety date time
		/// </summary>
		/// <param name="defMsg">The deferal message from the SMTP server.</param>
		/// <param name="ipAddress">IP Address that send was attempted from.</param>
		/// <param name="mxRecord">MX Record of the server tried to send too.</param>
		/// <param name="isServiceUnavailable">If false will backoff the retry, if true will use the MtaParameters.MtaRetryInterval, 
		/// this is needed to reduce the tail when sending as a message could get multiple try again laters and soon be 1h+ before next retry.</param>
		public void HandleDeliveryDeferral(string defMsg, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord, bool isServiceUnavailable = false)
		{
			HandleDeliveryDeferralAsync(defMsg, ipAddress, mxRecord, isServiceUnavailable).Wait();
		}

		/// <summary>
		/// Discards the message.
		/// </summary>
		/// <param name="failMsg"></param>
		public async Task<bool> HandleMessageDiscardAsync()
		{
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Discarded, string.Empty, null, null);
			IsHandled = true;
			return true;
		}

		/// <summary>
		/// This method handles message deferal.
		///	Logs deferral
		///	Fails the message if timed out
		/// or
		/// Sets the next rety date time
		/// </summary>
		/// <param name="defMsg">The deferal message from the SMTP server.</param>
		/// <param name="ipAddress">IP Address that send was attempted from.</param>
		/// <param name="mxRecord">MX Record of the server tried to send too.</param>
		/// <param name="isServiceUnavailable">If false will backoff the retry, if true will use the MtaParameters.MtaRetryInterval, 
		/// this is needed to reduce the tail when sending as a message could get multiple try again laters and soon be 1h+ before next retry.</param>
		public async Task<bool> HandleDeliveryDeferralAsync(string defMsg, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord, bool isServiceUnavailable = false, int? overrideTimeminutes = null)
		{
			// Log the deferral.
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Deferred, defMsg, ipAddress, mxRecord);

			// This holds the maximum interval between send retries. Should be put in the database.
			int maxInterval = 3 * 60;

			// Increase the defered count as the queued messages has been deferred.
			DeferredCount++;

			// Hold the minutes to wait until next retry.
			double nextRetryInterval = MtaParameters.MtaRetryInterval;

			if (overrideTimeminutes.HasValue)
			{
				nextRetryInterval = overrideTimeminutes.Value;
			}
			else
			{
				if (!isServiceUnavailable)
				{
					// Increase the deferred wait interval by doubling for each retry.
					for (int i = 1; i < DeferredCount; i++)
						nextRetryInterval = nextRetryInterval * 2;

					// If we have gone over the max interval then set to the max interval value.
					if (nextRetryInterval > maxInterval)
						nextRetryInterval = maxInterval;
				}
				else
					nextRetryInterval = 1; // For service unavalible use 1 minute between retries.
			}

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(nextRetryInterval);
			Requeue();

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="mxRecord"></param>
		/// <returns></returns>
		public async Task<bool> HandleFailedToConnectAsync(VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			// If there was no MX record in DNS, so using A, we should fail and not retry.
			if(mxRecord.MxRecordSrc == DNS.MxRecordSrc.A)
				return await HandleDeliveryFailAsync("550 Failed to connect", ipAddress, mxRecord);
			else
				return await HandleDeliveryDeferralAsync("Failed to connect", ipAddress, mxRecord, false, 15);
		}

		/// <summary>
		/// This method handle successful delivery.
		/// Logs success
		/// Deletes queued data
		/// </summary>
		public async Task<bool> HandleDeliverySuccessAsync(VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord, string response)
		{
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Success, response, ipAddress, mxRecord);
			IsHandled = true;
			return true;
		}

		/// <summary>
		/// This method handles message throttle.
		///	Logs throttle
		/// Sets the next rety date time 
		/// </summary>
		internal async Task<bool> HandleDeliveryThrottleAsync(VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			// Log deferral
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Throttled, string.Empty, ipAddress, mxRecord);

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
			Requeue();
			return true;
		}

		/// <summary>
		/// Handles a service unavailable event, should be same as defer but only wait 1 minute before next retry.
		/// </summary>
		/// <param name="sndIpAddress"></param>
		internal async Task<bool> HandleServiceUnavailableAsync(VirtualMta.VirtualMTA ipAddress)
		{
			// Log deferral
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Deferred, "Service Unavailable", ipAddress, null);

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddSeconds(15);
			Requeue();
			return true;
		}

		/// <summary>
		/// This method handles failure of delivery.
		/// Logs failure
		/// Deletes queued data
		/// </summary>
		/// <param name="failMsg"></param>
		public async Task<bool> HandleDeliveryFailAsync(string failMsg, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Failed, failMsg, ipAddress, mxRecord);

			try
			{
				// Send fails to Manta.Core.Events
				for (int i = 0; i < base.RcptTo.Length; i++)
				{
					EmailProcessingDetails processingInfo = null;
					Events.EventsManager.Instance.ProcessSmtpResponseMessage(failMsg, base.RcptTo[i], base.InternalSendID, out processingInfo);
				}
			}
			catch (Exception)
			{

			}

			IsHandled = true;

			return true;
		}

		/// <summary>
		/// Handle the message for a paused send.
		/// Should increase attempt send after timestamp and requeue in RabbitMQ.
		/// </summary>
		internal void HandleSendPaused()
		{
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
			Requeue();
		}

		/// <summary>
		/// Requeue the message in RabbitMQ.
		/// </summary>
		private void Requeue()
		{
			RabbitMqOutboundQueueManager.Enqueue(this);
			IsHandled = true;
		}
	}
}
