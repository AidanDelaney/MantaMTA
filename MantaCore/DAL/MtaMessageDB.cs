using MantaMTA.Core.Client.BO;
using MantaMTA.Core.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

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
		internal static async Task<bool> SaveAsync(MtaMessageSql message)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
	INSERT INTO man_mta_msg(mta_msg_id, mta_send_internalId, mta_msg_rcptTo, mta_msg_mailFrom)
	VALUES(@msgID, @internalSendID, @rcptTo, @mailFrom)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@internalSendID", message.InternalSendID);
				cmd.Parameters.AddWithValue("@rcptTo", string.Join<string>(_RcptToDelimiter, from rcpt in message.RcptTo select rcpt.Address));
				if (message.MailFrom == null)
					cmd.Parameters.AddWithValue("@mailFrom", DBNull.Value);
				else
					cmd.Parameters.AddWithValue("@mailFrom", message.MailFrom.Address);

				await conn.OpenAsync();
				await cmd.ExecuteNonQueryAsync();
				return true;
			}
		}

		/// <summary>
		/// Saves the Mta Queued message to the Database.
		/// </summary>
		/// <param name="message"></param>
		internal static async Task<bool> SaveAsync(MtaQueuedMessageSql message)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS(SELECT 1 FROM man_mta_queue WHERE mta_msg_id = @msgID)
	UPDATE man_mta_queue
	SET mta_queue_attemptSendAfter = @sendAfter,
	mta_queue_isPickupLocked = @isPickupLocked,
	mta_queue_dataPath = @dataPath,
	ip_group_id = @groupID
	WHERE mta_msg_id = @msgID
ELSE
	INSERT INTO man_mta_queue(mta_msg_id, mta_queue_queuedTimestamp, mta_queue_attemptSendAfter, mta_queue_isPickupLocked, mta_queue_dataPath, ip_group_id, mta_send_internalId, mta_queue_data)
	VALUES(@msgID, @queued, @sendAfter, @isPickupLocked, @dataPath, @groupID, @sendInternalID, @data)";
				cmd.Parameters.AddWithValue("@msgID", message.ID);
				cmd.Parameters.AddWithValue("@queued", message.QueuedTimestampUtc);
				cmd.Parameters.AddWithValue("@sendAfter", message.AttemptSendAfterUtc);
				cmd.Parameters.AddWithValue("@isPickupLocked", message.IsPickUpLocked);
				cmd.Parameters.AddWithValue("@dataPath", message.DataPath);
				cmd.Parameters.AddWithValue("@groupID", message.IPGroupID);
				cmd.Parameters.AddWithValue("@sendInternalID", message.InternalSendID);
				cmd.Parameters.AddWithValue("@data", message.Data);


				await conn.OpenAsync();
				await cmd.ExecuteNonQueryAsync();
				return true;
			}
		}

		/// <summary>
		/// Releases the Pickup log flag for specified message.
		/// </summary>
		/// <param name="messageID"></param>
		internal static void ReleasePickupLock(Guid messageID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
	UPDATE man_mta_queue
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
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
BEGIN TRANSACTION

DECLARE @msgIdTbl table(msgID uniqueidentifier)

;WITH Queue AS (
	SELECT	[que].mta_msg_id, 
			[que].mta_queue_attemptSendAfter, 
			ROW_NUMBER() OVER(	PARTITION BY [snd].mta_send_internalId 
								ORDER BY [que].mta_queue_attemptSendAfter ASC	) as 'RowNum'
	FROM man_mta_queue AS [que] WITH (READPAST)
		JOIN man_mta_send AS [snd] WITH (NOLOCK) ON [que].mta_send_internalId = [snd].mta_send_internalId 
	WHERE [que].mta_queue_attemptSendAfter <= GETUTCDATE()
		AND [snd].mta_sendStatus_id = 1
		AND [que].mta_queue_isPickupLocked = 0
)
INSERT INTO @msgIdTbl
SELECT TOP " + maxMessages + @" [queue].mta_msg_id
FROM Queue
ORDER BY [Queue].RowNum, [Queue].mta_queue_attemptSendAfter

UPDATE man_mta_queue
SET mta_queue_isPickupLocked = 1
WHERE mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

SELECT (SELECT COUNT(*)
		FROM man_mta_transaction as [tran] WITH (READPAST) 
		WHERE [tran].mta_msg_id = [msg].mta_msg_id
		AND [tran].mta_transactionStatus_id = 1) as 'DeferredCount',
		[msg].*, [que].mta_queue_attemptSendAfter, que.mta_queue_isPickupLocked, que.mta_queue_queuedTimestamp, que.mta_queue_dataPath, que.ip_group_id, que.mta_queue_data
FROM man_mta_queue as [que] WITH (READPAST)
JOIN man_mta_msg as [msg] WITH (READPAST) ON [que].[mta_msg_id] = [msg].[mta_msg_id]
WHERE [que].mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

COMMIT TRANSACTION";
				cmd.Parameters.AddWithValue("@sendStatus", (int)SendStatus.Active);
				List<MtaQueuedMessageSql> results = DataRetrieval.GetCollectionFromDatabase<MtaQueuedMessageSql>(cmd, CreateAndFillQueuedMessage);
				return new MtaQueuedMessageCollection(results);
			}
		}

		/// <summary>
		/// Gets messages that should be discarded.
		/// </summary>
		/// <param name="maxMessages">The maximum amount of messages get.</param>
		/// <returns>Messages for discarding.</returns>
		internal static MtaQueuedMessageCollection PickupForDiscarding(int maxMessages)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
BEGIN TRANSACTION

DECLARE @msgIdTbl table(msgID uniqueidentifier)

INSERT INTO @msgIdTbl
SELECT TOP " + maxMessages + @" [queue].mta_msg_id
FROM man_mta_queue as [queue]
JOIN man_mta_msg as [msg] on [queue].mta_msg_id = [msg].mta_msg_id
JOIN man_mta_send as [snd] on [msg].mta_send_internalId = [snd].mta_send_internalId
WHERE mta_queue_isPickupLocked = 0
AND mta_sendStatus_id = @sendStatus
ORDER BY mta_queue_attemptSendAfter ASC

UPDATE man_mta_queue
SET mta_queue_isPickupLocked = 1
WHERE mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

SELECT 0 as 'DeferredCount', [msg].*, [que].mta_queue_attemptSendAfter, que.mta_queue_isPickupLocked, que.mta_queue_queuedTimestamp, que.mta_queue_dataPath, que.ip_group_id, que.mta_queue_data
FROM man_mta_queue as [que]
JOIN man_mta_msg as [msg] ON [que].[mta_msg_id] = [msg].[mta_msg_id]
WHERE [que].mta_msg_id IN (SELECT msgID FROM @msgIdTbl)

COMMIT TRANSACTION";
				cmd.Parameters.AddWithValue("@sendStatus", (int)SendStatus.Discard);
				List<MtaQueuedMessageSql> results = DataRetrieval.GetCollectionFromDatabase<MtaQueuedMessageSql>(cmd, CreateAndFillQueuedMessage);
				return new MtaQueuedMessageCollection(results);
			}
		}
		
		/// <summary>
		/// Deletes the MtaQueuedMessage from the database.
		/// </summary>
		/// <param name="mtaQueuedMessage"></param>
		internal static async Task<bool> DeleteAsync(MtaQueuedMessageSql mtaQueuedMessage)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
	DELETE FROM man_mta_queue
	WHERE mta_msg_id = @msgID";
				cmd.Parameters.AddWithValue("@msgID", mtaQueuedMessage.ID);
				await conn.OpenAsync();
				await cmd.ExecuteNonQueryAsync();
				return true;
			}
		}

		/// <summary>
		/// Gets a MtaMessage from the database with the specified ID.
		/// </summary>
		/// <param name="messageID">ID of the message to get.</param>
		/// <returns>The MtaMessage if it exists otherwise null.</returns>
		internal static MtaMessageSql GetMtaMessage(Guid messageID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_mta_msg
WHERE mta_msg_id = @msgID";
				cmd.Parameters.AddWithValue("@msgID", messageID);
				return DataRetrieval.GetSingleObjectFromDatabase<MtaMessageSql>(cmd, CreateAndFillMessage);
			}
		}

		/// <summary>
		/// Creates and fills a MtaQueuedMessage from the IDataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaQueuedMessageSql CreateAndFillQueuedMessage(IDataRecord record)
		{
			MtaQueuedMessageSql qMsg = new MtaQueuedMessageSql(CreateAndFillMessage(record),
														 record.GetDateTime("mta_queue_queuedTimestamp"),
														 record.GetDateTime("mta_queue_attemptSendAfter"),
														 record.GetBoolean("mta_queue_isPickupLocked"),
														 record.GetStringOrEmpty("mta_queue_dataPath"),
														 record.GetInt32("ip_group_id"),
														 record.GetInt32("DeferredCount"),
														 record.GetStringOrEmpty("mta_queue_data"));
			return qMsg;
		}

		/// <summary>
		/// Creates and fills a MtaMessage from the IDataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaMessageSql CreateAndFillMessage(IDataRecord record)
		{
			MtaMessageSql msg = new MtaMessageSql();
			
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
