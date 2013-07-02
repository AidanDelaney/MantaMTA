using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using MantaMTA.Core.DAL;

namespace MantaMTA.Core.Client.BO
{
	/// <summary>
	/// Holds a single message for the MTA.
	/// </summary>
	internal class MtaMessage
	{
		/// <summary>
		/// Unique identifier for this message.
		/// </summary>
		public Guid ID { get; set; }
		/// <summary>
		/// Internal ID that identifies the Send that this 
		/// message is part of.
		/// </summary>
		public int InternalSendID { get; set; }
		/// <summary>
		/// The Mail From to used when sending this message.
		/// May be NULL for NullSender
		/// </summary>
		public MailAddress MailFrom { get; set; }
		/// <summary>
		/// Array of Rcpt To's for this message.
		/// </summary>
		public MailAddress[] RcptTo { get; set; }

		/// <summary>
		/// Save this MTA message to the Database.
		/// </summary>
		public void Save()
		{
			MtaMessageDB.Save(this);
		}

		/// <summary>
		/// Creates and MTA message from the passed in parameters.
		/// </summary>
		/// <param name="internalSendID">The internal ID of the Send this message is part of.</param>
		/// <param name="mailFrom">Mail From used in SMTP.</param>
		/// <param name="rcptTo">Rcpt To's used in SMTP.</param>
		/// <returns></returns>
		public static MtaMessage Create(int internalSendID, string mailFrom, string[] rcptTo)
		{
			MtaMessage mtaMessage = new MtaMessage();
			mtaMessage.ID = Guid.NewGuid();
			mtaMessage.InternalSendID = internalSendID;
			
			if (mailFrom != null)
				mtaMessage.MailFrom = new MailAddress(mailFrom);
			else
				mtaMessage.MailFrom = null;

			mtaMessage.RcptTo = new MailAddress[rcptTo.Length];
			for (int i = 0; i < rcptTo.Length; i++)
				mtaMessage.RcptTo[i] = new MailAddress(rcptTo[i]);

			mtaMessage.Save();

			return mtaMessage;
		}

		/// <summary>
		/// Queue the message.
		/// </summary>
		/// <returns></returns>
		public virtual MtaQueuedMessage Queue(string data, int ipGroupID)
		{
			string dataPath = Path.Combine(MtaParameters.MTA_QUEUEFOLDER, ID + ".eml"); 
			MtaQueuedMessage qMsg = new MtaQueuedMessage(this, DateTime.UtcNow, DateTime.UtcNow, false, dataPath, ipGroupID);

			using (StreamWriter writer = new StreamWriter(dataPath))
			{
				writer.Write(data);
			}
			
			MtaMessageDB.Save(qMsg);
			return qMsg;
		}
	}

	/// <summary>
	/// Holds a collection of MtaMessages.
	/// </summary>
	internal class MtaMessageCollection : List<MtaMessage>
	{
		public MtaMessageCollection(IEnumerable<MtaMessage> collection) : base(collection) { }
		public MtaMessageCollection() { }
	}
}
