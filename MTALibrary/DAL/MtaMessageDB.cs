using System.Configuration;
using System.Data.SqlClient;
using Colony101.MTA.Library.Client.BO;
using System.Linq;
using System;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;

namespace Colony101.MTA.Library.DAL
{
	internal static class MtaMessageDB
	{
		private const string _RcptToDelimiter = ",";

		/// <summary>
		/// Save the MTA Message to the database.
		/// </summary>
		/// <param name="message"></param>
		internal static void Save(MtaMessage message)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS(SELECT 1 FROM c101_mta_msg WHERE mta_msg_id = @msgID)
	UPDATE c101_mta_msg
	SET mta_msg_rcptTo = @rcptTo,
	mta_msg_mailFrom = @mailFrom,
	mta_msg_dataPath = @dataPath,
	mta_msg_outboundIP = @outboundIP
	WHERE mta_msg_id = @msgID
ELSE
	INSERT INTO c101_mta_msg(mta_msg_id, mta_msg_rcptTo, mta_msg_mailFrom, mta_msg_dataPath, mta_msg_outboundIP)
	VALUES(@msgID, @rcptTo, @mailFrom, @dataPath, @outboundIP)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@rcptTo", string.Join<string>(_RcptToDelimiter, from rcpt in message.RcptTo select rcpt.Address));
				cmd.Parameters.AddWithValue("@mailFrom", message.MailFrom.Address);
				cmd.Parameters.AddWithValue("@dataPath", message.DataPath);
				cmd.Parameters.AddWithValue("@outboundIP", message.OutboundIP);

				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Saves the Mta Queued message to the Database.
		/// </summary>
		/// <param name="message"></param>
		internal static void Save(MtaQueuedMessage message)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS(SELECT 1 FROM c101_mta_queue WHERE mta_msg_id = @msgID)
	UPDATE c101_mta_queue
	SET mta_queue_attemptSendAfter = @sendAfter,
	mta_queue_isPickupLocked = @isPickupLocked
	WHERE mta_msg_id = @msgID
ELSE
	INSERT INTO c101_mta_queue(mta_msg_id, mta_queue_queuedTimestamp, mta_queue_attemptSendAfter, mta_queue_isPickupLocked)
	VALUES(@msgID, @queued, @sendAfter, @isPickupLocked)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@queued", message.QueuedTimestamp);
				cmd.Parameters.AddWithValue("@sendAfter", message.AttemptSendAfter);
				cmd.Parameters.AddWithValue("@isPickupLocked", message.IsPickUpLocked);

				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Releases the Pickup log flag for specified message.
		/// </summary>
		/// <param name="messageID"></param>
		internal static void ReleasePickupLock(Guid messageID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
	UPDATE c101_mta_queue
	SET mta_queue_isPickupLocked = 0
	WHERE mta_msg_id = @msgID";
				cmd.Parameters.AddWithValue("@msgID", messageID);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		internal static MtaQueuedMessageCollection PickupForSending(int maxMessages)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
--// No other transactions can modify data that has been read by the current transaction until the current transaction completes.
--// Needed to prevent a queued message being picked up more than once.
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
BEGIN TRANSACTION

DECLARE @msgIdTbl table(msgID uniqueidentifier)

INSERT INTO @msgIdTbl
SELECT TOP " + maxMessages + @" mta_msg_id
FROM c101_mta_queue
WHERE mta_queue_attemptSendAfter < GETDATE()
AND mta_queue_isPickupLocked = 0
ORDER BY mta_queue_attemptSendAfter ASC

UPDATE c101_mta_queue
SET mta_queue_isPickupLocked = 1
WHERE mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

SELECT [msg].*, [que].mta_queue_attemptSendAfter, que.mta_queue_isPickupLocked, que.mta_queue_queuedTimestamp
FROM c101_mta_queue as [que]
JOIN c101_mta_msg as [msg] ON [que].[mta_msg_id] = [msg].[mta_msg_id]
WHERE [que].mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

COMMIT TRANSACTION";
				List<MtaQueuedMessage> results = DataRetrieval.GetCollectionFromDatabase<MtaQueuedMessage>(cmd, CreateAndFillQueuedMessage);
				return new MtaQueuedMessageCollection(results);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mtaQueuedMessage"></param>
		internal static void Delete(MtaQueuedMessage mtaQueuedMessage)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
	DELETE FROM c101_mta_queue
	WHERE mta_msg_id = @msgID";
				cmd.Parameters.AddWithValue("@msgID", mtaQueuedMessage.ID);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaQueuedMessage CreateAndFillQueuedMessage(IDataRecord record)
		{
			MtaQueuedMessage qMsg = new MtaQueuedMessage(CreateAndFillMessage(record),
														 record.GetDateTime("mta_queue_queuedTimestamp"),
														 record.GetDateTime("mta_queue_attemptSendAfter"),
														 record.GetBoolean("mta_queue_isPickupLocked"));
			return qMsg;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaMessage CreateAndFillMessage(IDataRecord record)
		{
			MtaMessage msg = new MtaMessage();
			msg.DataPath = record.GetString("mta_msg_dataPath");
			msg.ID = record.GetGuid("mta_msg_id");
			msg.MailFrom = new MailAddress(record.GetString("mta_msg_mailFrom"));
			msg.OutboundIP = record.GetString("mta_msg_outboundIP");

			// Get the recipients.
			msg.RcptTo = (from r
						  in record.GetString("mta_msg_rcptTo").Split(_RcptToDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries) 
						  select new MailAddress(r)).ToArray();

			return msg;
		}
	}
}
