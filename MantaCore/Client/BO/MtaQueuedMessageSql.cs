using System;
using System.Collections.Generic;
using System.IO;
using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;
using MantaMTA.Core.Events;
using System.Threading.Tasks;

namespace MantaMTA.Core.Client.BO
{
	/// <summary>
	/// Holds a QUEUED MtaMesage
	/// </summary>
	internal class MtaQueuedMessageSql : MtaMessageSql, IDisposable
	{
		/// <summary>
		/// Timestamp of when the message was originally queued.
		/// </summary>
		public DateTime QueuedTimestampUtc { get; set; }
		/// <summary>
		/// Timestamp of the earliest the first/next attempt to send the message should be made.
		/// </summary>
		public DateTime AttemptSendAfterUtc { get; set; }
		/// <summary>
		/// The amount of times this message has been deferred.
		/// </summary>
		public int DeferredCount { get; set; }
		/// <summary>
		/// This should replicate the database value. It should never directly be messed with.
		/// </summary>
		private bool _IsPickUpLocked { get; set; }
		/// <summary>
		/// If TRUE this should be the only instance of the Queue Message.
		/// </summary>
		public bool IsPickUpLocked
		{
			get
			{
				return _IsPickUpLocked;
			}
		}
		/// <summary>
		/// The path to the file containing this messages DATA.
		/// </summary>
		public string DataPath { get; set; }

		/// <summary>
		/// The Email data.
		/// </summary>
		public string Data { get; set; }

		/// <summary>
		/// Gets the DATA for this message. Is read from <paramref name="DataPath"/>
		/// If DataPath is empty will return string.Empty.
		/// </summary>
		public async Task<string> GetDataAsync()
		{
			// If the DATA path is empty then data is not in a file so can return Data.
			if (string.IsNullOrWhiteSpace(this.DataPath))
			{
				if (string.IsNullOrWhiteSpace(this.Data))
					return string.Empty;

				return this.Data;
			}

			// Read the data from the file.
			using (StreamReader reader = new StreamReader(this.DataPath))
			{
				return await reader.ReadToEndAsync();
			}
		}

		/// <summary>
		/// The IP address used to send this Message.
		/// </summary>
		public int IPGroupID { get; set; }

		/// <summary>
		/// Create a Queued message object using the passed in parameters.
		/// </summary>
		/// <param name="message">The MtaMessage that this queued message should be based on.</param>
		/// <param name="queuedTimestampUtc">The original queued timestamp.</param>
		/// <param name="attemptSendAfterUtc">The date time before which to no attempts should be made to send.</param>
		/// <param name="isPickUpLocked">TRUE only if the field in database is also true.</param>
		/// <param name="dataPath">Path to the email file.</param>
		/// <param name="ipGroupID">ID of the IP Group to send through.</param>
		/// <param name="deferredCount">Ammount of times the message has been deferred.</param>
		/// <param name="data">The Email Data, if not stored in a file.</param>
		public MtaQueuedMessageSql(MtaMessageSql message, DateTime queuedTimestampUtc, DateTime attemptSendAfterUtc, bool isPickUpLocked, string dataPath, int ipGroupID, int deferredCount, string data = "")
		{
			base.ID = message.ID;
			base.MailFrom = message.MailFrom;
			base.RcptTo = message.RcptTo;
			base.InternalSendID = message.InternalSendID;

			QueuedTimestampUtc = queuedTimestampUtc;
			AttemptSendAfterUtc = attemptSendAfterUtc;
			_IsPickUpLocked = isPickUpLocked;
			DataPath = dataPath;
			IPGroupID = ipGroupID;
			DeferredCount = deferredCount;
			Data = data;
		}

		/// <summary>
		/// If the queued message is locked make sure to release it when disposing.
		/// </summary>
		public void Dispose()
		{
			if (_IsPickUpLocked)
				ReleasePickupLock();
		}

		/// <summary>
		/// Releases the Pickup lock allowing the message to be picked up.
		/// </summary>
		public void ReleasePickupLock()
		{
			MtaMessageDB.ReleasePickupLock(base.ID);
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

			// Send fails to Manta.Core.Events
			try
			{
				for (int i = 0; i < base.RcptTo.Length; i++)
				{
					EmailProcessingDetails processingInfo = null;
					Events.EventsManager.Instance.ProcessSmtpResponseMessage(failMsg, base.RcptTo[i], base.InternalSendID, out processingInfo);
				}
			}
			catch (Exception)
			{

			}
			
			await MtaMessageDB.DeleteAsync(this);
			DeleteMessageData();
			return true;
		}

		/// <summary>
		/// This method handles failer of devlivery.
		/// Logs failer
		/// Deletes queued data
		/// </summary>
		/// <param name="failMsg"></param>
		public void HandleMessageDiscard()
		{
			MtaTransaction.LogTransaction(this, TransactionStatus.Discarded, string.Empty, null, null);
			MtaMessageDB.DeleteAsync(this).Wait();
			DeleteMessageData();
		}

		/// <summary>
		/// This method handle successful delivery.
		/// Logs success
		/// Deletes queued data
		/// </summary>
		public async Task<bool> HandleDeliverySuccessAsync(VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Success, string.Empty, ipAddress, mxRecord);
			await MtaMessageDB.DeleteAsync(this);
			DeleteMessageData();
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
		public async Task<bool> HandleDeliveryDeferralAsync(string defMsg, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord, bool isServiceUnavailable = false)
		{
			// Log the deferral.
			await MtaTransaction.LogTransactionAsync(this, TransactionStatus.Deferred, defMsg, ipAddress, mxRecord);
		
			// This holds the maximum interval between send retries. Should be put in the database.
			int maxInterval = 3 * 60;
			
			// Increase the defered count as the queued messages has been deferred.
			DeferredCount++;

			// Hold the minutes to wait until next retry.
			double nextRetryInterval = MtaParameters.MtaRetryInterval;

			if (!isServiceUnavailable)
			{
				// Increase the deferred wait interval by doubling for each retry.
				for (int i = 1; i < DeferredCount; i++)
					nextRetryInterval = nextRetryInterval * 2;

				// If we have gone over the max interval then set to the max interval value.
				if (nextRetryInterval > maxInterval)
					nextRetryInterval = maxInterval;
			}

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(nextRetryInterval);
			await MtaMessageDB.SaveAsync(this);

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
		public void HandleDeliveryDeferral(string defMsg, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord, bool isServiceUnavailable = false)
		{
			HandleDeliveryDeferralAsync(defMsg, ipAddress, mxRecord, isServiceUnavailable).Wait();
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
			await MtaMessageDB.SaveAsync(this);
			return true;
		}

		/// <summary>
		/// Delete the DATA for this message.
		/// </summary>
		public void DeleteMessageData()
		{
			if (File.Exists(this.DataPath))
			{
				File.Delete(this.DataPath);
				this.DataPath = string.Empty;
			}
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
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
			await MtaMessageDB.SaveAsync(this);
			return true;
		}
	}

	/// <summary>
	/// Holds a collection of MtaQueuedMessage.
	/// </summary>
	internal class MtaQueuedMessageCollection : List<MtaQueuedMessageSql>
	{
		public MtaQueuedMessageCollection() { }
		public MtaQueuedMessageCollection(IEnumerable<MtaQueuedMessageSql> collection) : base(collection) { }
	}
}
