using MantaMTA.Core.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MantaMTA.Core.Client.BO
{
	/// <summary>
	/// Holds a single message for the MTA.
	/// </summary>
	internal class MtaMessageSql : MtaMessage
	{
		/// <summary>
		/// Save this MTA message to the Database.
		/// </summary>
		public async Task<bool> SaveAsync()
		{
			await MtaMessageDB.SaveAsync(this);
			return true;
		}

		/// <summary>
		/// Creates and MTA message from the passed in parameters.
		/// </summary>
		/// <param name="internalSendID">The internal ID of the Send this message is part of.</param>
		/// <param name="mailFrom">Mail From used in SMTP.</param>
		/// <param name="rcptTo">Rcpt To's used in SMTP.</param>
		/// <returns></returns>
		public static async Task<MtaMessageSql> CreateAsync(Guid messageID, int internalSendID, string mailFrom, string[] rcptTo)
		{
			MtaMessageSql mtaMessage = new MtaMessageSql();
			mtaMessage.ID = messageID;
			mtaMessage.InternalSendID = internalSendID;
			mtaMessage.MailFrom = mailFrom;
			mtaMessage.RcptTo = rcptTo;
			await mtaMessage.SaveAsync();
			return mtaMessage;
		}

		/// <summary>
		/// Queue the message.
		/// </summary>
		/// <returns></returns>
		public async Task<MtaQueuedMessageSql> QueueAsync(string data, int ipGroupID)
		{
			string dataPath = Path.Combine(MtaParameters.MTA_QUEUEFOLDER, ID + ".eml");

			if (File.Exists(dataPath))
				throw new IOException();

			MtaQueuedMessageSql qMsg = new MtaQueuedMessageSql(this, DateTime.UtcNow, DateTime.UtcNow, false, dataPath, ipGroupID, 0);

			using (StreamWriter writer = new StreamWriter(dataPath))
			{
				await writer.WriteAsync(data);
			}
			
			await MtaMessageDB.SaveAsync(qMsg);
			return qMsg;
		}
	}

	/// <summary>
	/// Holds a collection of MtaMessages.
	/// </summary>
	internal class MtaMessageSqlCollection : List<MtaMessageSql>
	{
		public MtaMessageSqlCollection(IEnumerable<MtaMessageSql> collection) : base(collection) { }
		public MtaMessageSqlCollection() { }
	}
}
