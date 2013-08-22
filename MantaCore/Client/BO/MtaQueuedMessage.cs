using System;
using System.Collections.Generic;
using System.IO;
using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;

namespace MantaMTA.Core.Client.BO
{
	/// <summary>
	/// Holds a QUEUED MtaMesage
	/// </summary>
	internal class MtaQueuedMessage : MtaMessage, IDisposable
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
		/// The DATA for this message. Is read from <paramref name="DataPath"/>
		/// If DataPath is empty, will throw exception.
		/// </summary>
		public string Data
		{
			get
			{
				// If the DATA path is empty, then Data shouldn't be called.
				if (string.IsNullOrWhiteSpace(this.DataPath))
					throw new FileNotFoundException("Data doesn't exist.");

				using (StreamReader reader = new StreamReader(this.DataPath))
				{
					return reader.ReadToEnd();
				}
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
		public MtaQueuedMessage(MtaMessage message, DateTime queuedTimestampUtc, DateTime attemptSendAfterUtc, bool isPickUpLocked, string dataPath, int ipGroupID, int deferredCount)
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
		/// This method handles failer of devlivery.
		/// Logs failer
		/// Deletes queued data
		/// </summary>
		/// <param name="failMsg"></param>
		public void HandleDeliveryFail(string failMsg, MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Failed, failMsg, ipAddress, mxRecord);
			// Send fails to Manta.Core.Events
			try
			{
				for (int i = 0; i < base.RcptTo.Length; i++)
					Events.EventsManager.Instance.ProcessSmtpResponseMessage(failMsg, base.RcptTo[i].Address, base.InternalSendID);
			}
			catch (Exception)
			{

			}
			MtaMessageDB.Delete(this);
			DeleteMessageData();
		}

		/// <summary>
		/// This method handles failer of devlivery.
		/// Logs failer
		/// Deletes queued data
		/// </summary>
		/// <param name="failMsg"></param>
		public void HandleMessageDiscard()
		{
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Discarded, string.Empty, null, null);
			MtaMessageDB.Delete(this);
			DeleteMessageData();
		}

		/// <summary>
		/// This method handle successful delivery.
		/// Logs success
		/// Deletes queued data
		/// </summary>
		public void HandleDeliverySuccess(MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Success, string.Empty, ipAddress, mxRecord);
			MtaMessageDB.Delete(this);
			DeleteMessageData();
		}

		/// <summary>
		/// This method handles message deferal.
		///	Logs deferral
		///	Fails the message if timed out
		/// or
		/// Sets the next rety date time
		/// </summary>
		/// <param name="defMsg"></param>
		public void HandleDeliveryDeferral(string defMsg, MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			// Log the deferral.
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Deferred, defMsg, ipAddress, mxRecord);
		
			// This holds the maximum interval between send retries. Should be put in the database.
			int maxInterval = 3 * 60;
			
			// Increase the defered count as the queued messages has been deferred.
			DeferredCount++;

			// Hold the minutes to wait until next retry.
			double nextRetryInterval = MtaParameters.MtaRetryInterval;

			// Increase the deferred wait interval by doubling for each retry.
			for (int i = 1; i < DeferredCount; i++)
					nextRetryInterval = nextRetryInterval * 2;

			// If we have gone over the max interval then set to the max interval value.
			if (nextRetryInterval > maxInterval)
				nextRetryInterval = maxInterval;

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(nextRetryInterval);
			MtaMessageDB.Save(this);
		}

		/// <summary>
		/// This method handles message throttle.
		///	Logs throttle
		/// Sets the next rety date time 
		/// </summary>
		internal void HandleDeliveryThrottle(MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			// Log deferral
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Throttled, string.Empty, ipAddress, mxRecord);

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
			MtaMessageDB.Save(this);
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
		/// Handles a service unavailable event, should be same as deffer but only wait 1 minute before next retry.
		/// </summary>
		/// <param name="sndIpAddress"></param>
		internal void HandleServiceUnavailable(MtaIpAddress.MtaIpAddress ipAddress)
		{
			// Log deferral
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Deferred, "Service Unavalible", ipAddress, null);

			// Set next retry time and release the lock.
			this.AttemptSendAfterUtc = DateTime.UtcNow.AddMinutes(1);
			MtaMessageDB.Save(this);
		}
	}

	/// <summary>
	/// Holds a collection of MtaQueuedMessage.
	/// </summary>
	internal class MtaQueuedMessageCollection : List<MtaQueuedMessage>
	{
		public MtaQueuedMessageCollection() { }
		public MtaQueuedMessageCollection(IEnumerable<MtaQueuedMessage> collection) : base(collection) { }
	}
}
