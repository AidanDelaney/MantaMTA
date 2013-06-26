using System.Configuration;
using System.Data.SqlClient;
using MantaMTA.Core.Client.BO;
using System.Linq;
using System;
using System.Data;
using System.Net.Mail;
using System.Collections.Generic;

namespace MantaMTA.Core.DAL
{
	internal static class MtaMessageDB
	{
		/// <summary>
		/// Delimiter user for RCPT addresses.
		/// </summary>
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
	SET mta_send_internalId = @internalSendID,
	mta_msg_rcptTo = @rcptTo,
	mta_msg_mailFrom = @mailFrom
	WHERE mta_msg_id = @msgID
ELSE
	INSERT INTO c101_mta_msg(mta_msg_id, mta_send_internalId, mta_msg_rcptTo, mta_msg_mailFrom)
	VALUES(@msgID, @internalSendID, @rcptTo, @mailFrom)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@internalSendID", message.InternalSendID);
				cmd.Parameters.AddWithValue("@rcptTo", string.Join<string>(_RcptToDelimiter, from rcpt in message.RcptTo select rcpt.Address));
				if (message.MailFrom == null)
					cmd.Parameters.AddWithValue("@mailFrom", DBNull.Value);
				else
					cmd.Parameters.AddWithValue("@mailFrom", message.MailFrom.Address);

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
	mta_queue_isPickupLocked = @isPickupLocked,
	mta_queue_dataPath = @dataPath,
	ip_group_id = @groupID
	WHERE mta_msg_id = @msgID
ELSE
	INSERT INTO c101_mta_queue(mta_msg_id, mta_queue_queuedTimestamp, mta_queue_attemptSendAfter, mta_queue_isPickupLocked, mta_queue_dataPath, ip_group_id)
	VALUES(@msgID, @queued, @sendAfter, @isPickupLocked, @dataPath, @groupID)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@queued", message.QueuedTimestamp);
				cmd.Parameters.AddWithValue("@sendAfter", message.AttemptSendAfter);
				cmd.Parameters.AddWithValue("@isPickupLocked", message.IsPickUpLocked);
				cmd.Parameters.AddWithValue("@dataPath", message.DataPath);
				cmd.Parameters.AddWithValue("@groupID", message.IPGroupID);

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
		/// Gets messages that are due to be sent.
		/// Not Threadsafe. If multiple calls are made to this method then messages could be picked up twice.
		/// </summary>
		/// <param name="maxMessages">The maximum amount of messages get.</param>
		/// <returns>Collection of messages queued for sending.</returns>
		internal static MtaQueuedMessageCollection PickupForSending(int maxMessages)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
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

SELECT [msg].*, [que].mta_queue_attemptSendAfter, que.mta_queue_isPickupLocked, que.mta_queue_queuedTimestamp, que.mta_queue_dataPath, que.ip_group_id
FROM c101_mta_queue as [que]
JOIN c101_mta_msg as [msg] ON [que].[mta_msg_id] = [msg].[mta_msg_id]
WHERE [que].mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

COMMIT TRANSACTION";
				List<MtaQueuedMessage> results = DataRetrieval.GetCollectionFromDatabase<MtaQueuedMessage>(cmd, CreateAndFillQueuedMessage);
				return new MtaQueuedMessageCollection(results);
			}
		}

		/// <summary>
		/// Deletes the MtaQueuedMessage from the database.
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
		/// Creates and fills a MtaQueuedMessage from the IDataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaQueuedMessage CreateAndFillQueuedMessage(IDataRecord record)
		{
			MtaQueuedMessage qMsg = new MtaQueuedMessage(CreateAndFillMessage(record),
														 record.GetDateTime("mta_queue_queuedTimestamp"),
														 record.GetDateTime("mta_queue_attemptSendAfter"),
														 record.GetBoolean("mta_queue_isPickupLocked"),
														 record.GetString("mta_queue_dataPath"),
														 record.GetInt32("ip_group_id"));
			return qMsg;
		}

		/// <summary>
		/// Creates and fills a MtaMessage from the IDataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaMessage CreateAndFillMessage(IDataRecord record)
		{
			MtaMessage msg = new MtaMessage();
			
			msg.ID = record.GetGuid("mta_msg_id");
			msg.InternalSendID = record.GetInt32("mta_send_internalId");
			if (!record.IsDBNull("mta_msg_mailFrom"))
				msg.MailFrom = new MailAddress(record.GetString("mta_msg_mailFrom"));
			else
				msg.MailFrom = null;

			// Get the recipients.
			msg.RcptTo = (from r
						  in record.GetString("mta_msg_rcptTo").Split(_RcptToDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries) 
						  select new MailAddress(r)).ToArray();

			return msg;
		}
	}
}
