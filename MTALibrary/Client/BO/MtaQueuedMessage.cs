using System;
using System.Collections.Generic;
using Colony101.MTA.Library.DAL;
using Colony101.MTA.Library.Enums;

namespace Colony101.MTA.Library.Client.BO
{
	/// <summary>
	/// Holds a QUEUED MtaMesage
	/// </summary>
	internal class MtaQueuedMessage : MtaMessage, IDisposable
	{
		/// <summary>
		/// Timestamp of when the message was originally queued.
		/// </summary>
		public DateTime QueuedTimestamp { get; set; }
		/// <summary>
		/// Timestamp of the earliest the first/next attempt to send the message should be made.
		/// </summary>
		public DateTime AttemptSendAfter { get; set; }
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
		/// Create a Queued message object using the passed in parameters.
		/// </summary>
		/// <param name="message">The MtaMessage that this queued message should be based on.</param>
		/// <param name="queuedTimestamp">The original queued timestamp.</param>
		/// <param name="attemptSendAfter">The date time before which to no attempts should be made to send.</param>
		/// <param name="isPickUpLocked">TRUE only if the field in database is also true.</param>
		public MtaQueuedMessage(MtaMessage message, DateTime queuedTimestamp, DateTime attemptSendAfter, bool isPickUpLocked)
		{
			base.DataPath = message.DataPath;
			base.ID = message.ID;
			base.MailFrom = message.MailFrom;
			base.OutboundIP = message.OutboundIP;
			base.RcptTo = message.RcptTo;

			QueuedTimestamp = queuedTimestamp;
			AttemptSendAfter = attemptSendAfter;
			_IsPickUpLocked = isPickUpLocked;
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
		public void HandleDeliveryFail(string failMsg)
		{
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Failed, failMsg);
			MtaMessageDB.Delete(this);
			base.DeleteMessageData();
		}

		/// <summary>
		/// This method handle successful delivery.
		/// Logs success
		/// Deletes queued data
		/// </summary>
		public void HandleDeliverySuccess()
		{
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Success, string.Empty);
			MtaMessageDB.Delete(this);
			base.DeleteMessageData();
		}

		/// <summary>
		/// This method handles message deferal.
		///	Logs deferral
		///	Fails the message if timed out
		/// or
		/// Sets the next rety date time
		/// </summary>
		/// <param name="defMsg"></param>
		public void HandleDeliveryDeferral(string defMsg)
		{
			// Log deferral
			MtaTransaction.LogTransaction(this.ID, TransactionStatus.Deferred, defMsg);

			// Set next retry time and release the lock.
			this.AttemptSendAfter = DateTime.Now.AddMinutes(MtaParameters.MTA_RETRY_INTERVAL);
			this._IsPickUpLocked = false;
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
