using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using WebInterfaceLib.BO;

namespace WebInterfaceLib.DAL
{
	public static class SendDB
	{
		/// <summary>
		/// Gets the amount of messages currently the queue with the specified statuses.
		/// </summary>
		/// <returns>Count of the messages waiting in the queue.</returns>
		public static long GetQueueCount(SendStatus[] sendStatus)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT SUM(s.mta_send_messages) - SUM(s.mta_send_accepted + s.mta_send_rejected)
FROM man_mta_send as s
WHERE s.mta_sendStatus_id in (" + string.Join(",", Array.ConvertAll<SendStatus, int>(sendStatus, s => (int)s)) + ")";
				conn.Open();
				return Convert.ToInt64(cmd.ExecuteScalar());
			}
		}

		/// <summary>
		/// Get a count of all the sends in the MantaMTA database.
		/// </summary>
		/// <returns>Count of all Sends.</returns>
		public static long GetSendsCount()
		{
			using (SqlConnection conn = MantaMTA.Core.DAL.MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT COUNT(*) FROM man_mta_send";
				conn.Open();
				return Convert.ToInt64(cmd.ExecuteScalar());
			}
		}

		/// <summary>
		/// Gets a page sends information.
		/// </summary>
		/// <param name="pageSize">Size of the page to get.</param>
		/// <param name="pageNum">The page to get.</param>
		/// <returns>SendInfoCollection of the data page.</returns>
		public static SendInfoCollection GetSends(int pageSize, int pageNum)
		{
			using (SqlConnection conn = MantaMTA.Core.DAL.MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"DECLARE @sends table (RowNum int, 
					  mta_send_internalId int)

INSERT INTO @sends
SELECT [sends].RowNumber, [sends].mta_send_internalId
FROM (SELECT (ROW_NUMBER() OVER(ORDER BY mta_send_createdTimestamp DESC)) as RowNumber, man_mta_send.mta_send_internalId
FROM man_mta_send) [sends]
WHERE [sends].RowNumber >= " + ((pageNum * pageSize) - pageSize + 1) + " AND [sends].RowNumber <= " + (pageSize * pageNum) + @"

SELECT [send].*, 
	mta_send_messages AS 'Messages',
	mta_send_accepted AS 'Accepted',
	mta_send_rejected AS 'Rejected',
	([send].mta_send_messages - (mta_send_accepted + mta_send_rejected)) AS 'Waiting',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred',
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] JOIN  man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId) AS 'LastTransactionTimestamp'
FROM man_mta_send as [send]
WHERE [send].mta_send_internalId in (SELECT [s].mta_send_internalId FROM @sends as [s])
ORDER BY [send].mta_send_createdTimestamp DESC";
				cmd.CommandTimeout = 90; // Query can take a while to run due to the size of the Transactions table.
				return new SendInfoCollection(DataRetrieval.GetCollectionFromDatabase<SendInfo>(cmd, CreateAndFillSendInfo));
			}
		}

		/// <summary>
		/// Gets all of the sends with messages waiting to be sent.
		/// </summary>
		/// <returns>SendInfoCollection of all relevent sends.</returns>
		public static SendInfoCollection GetSendsInProgress()
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT [send].*, 
	mta_send_messages AS 'Messages',
	mta_send_accepted AS 'Accepted',
	mta_send_rejected AS 'Rejected',
	([send].mta_send_messages - (mta_send_accepted + mta_send_rejected)) AS 'Waiting',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred',
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] JOIN  man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [send].mta_send_internalId) AS 'LastTransactionTimestamp'
FROM man_mta_send as [send]
WHERE ([send].mta_send_messages - (mta_send_accepted + mta_send_rejected)) > 0
ORDER BY [send].mta_send_createdTimestamp DESC";
				cmd.CommandTimeout = 90; // Query can take a while to run due to the size of the Transactions table.
				return new SendInfoCollection(DataRetrieval.GetCollectionFromDatabase<SendInfo>(cmd, CreateAndFillSendInfo));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sendID"></param>
		/// <returns></returns>
		public static SendInfo GetSend(string sendID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT [snd].*,
	(SELECT COUNT(*) FROM man_mta_msg WHERE man_mta_msg.mta_send_internalId = [snd].mta_send_internalId) AS 'Messages',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId AND [tran].mta_transactionStatus_id = 4) AS 'Accepted',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId AND ([tran].mta_transactionStatus_id = 2 OR [tran].mta_transactionStatus_id = 3 OR [tran].mta_transactionStatus_id = 6)) AS 'Rejected',
	(SELECT COUNT(*) FROM man_mta_queue as [queue] JOIN man_mta_msg as [msg] ON [queue].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId) AS 'Waiting',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred',
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] JOIN  man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [snd].mta_send_internalId) AS 'LastTransactionTimestamp'
FROM man_mta_send as [snd]
WHERE [snd].mta_send_id = @sndID";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				return DataRetrieval.GetSingleObjectFromDatabase<SendInfo>(cmd, CreateAndFillSendInfo);
			}
		}

		/// <summary>
		/// Gets a Sends Metadata from the database.
		/// </summary>
		/// <param name="sendID"></param>
		/// <returns></returns>
		public static SendMetadataCollection GetSendMetaData(int internalSendID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT *
FROM man_mta_sendMeta
WHERE mta_send_internalId = @sndID";
				cmd.Parameters.AddWithValue("@sndID", internalSendID);
				return new SendMetadataCollection(DataRetrieval.GetCollectionFromDatabase<SendMetadata>(cmd, CreateAndFillSendMetadata));
			}
		}

		/// <summary>
		/// Creates a send info object filled with data from the data record.
		/// </summary>
		/// <param name="record">Where to get the data to fill object from.</param>
		/// <returns>A populated SendInfo object.</returns>
		private static SendInfo CreateAndFillSendInfo(IDataRecord record)
		{
			SendInfo sInfo = new SendInfo
			{
				ID = record.GetString("mta_send_id"),
				InternalID = record.GetInt32("mta_send_internalId"),
				SendStatus = (SendStatus)record.GetInt64("mta_sendStatus_id"),
				LastAccessedTimestamp = DateTime.UtcNow,
				CreatedTimestamp = record.GetDateTime("mta_send_createdTimestamp"),
				Accepted = record.GetInt64("Accepted"),
				Deferred = record.GetInt64("Deferred"),
				Rejected = record.GetInt64("Rejected"),
				Throttled = record.GetInt64("Throttled"),
				TotalMessages = record.GetInt64("Messages"),
				Waiting = record.GetInt64("Waiting")
			};

			if (!record.IsDBNull("LastTransactionTimestamp"))
				sInfo.LastTransactionTimestamp = record.GetDateTime("LastTransactionTimestamp");

			return sInfo;
		}

		/// <summary>
		/// Creates a send metadata object from the data record.
		/// </summary>
		/// <param name="record">Where to get the data to fill object from.</param>
		/// <returns>A populated SendMetadata object.</returns>
		private static SendMetadata CreateAndFillSendMetadata(IDataRecord record)
		{
			return new SendMetadata
			{
				Name = record.GetStringOrEmpty("mta_sendMeta_name"),
				Value = record.GetStringOrEmpty("mta_sendMeta_value")
			};
		}

		public static bool SaveSendMetadata(int internalSendID, SendMetadata metadata)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"IF EXISTS(SELECT 1
FROM man_mta_sendMeta
WHERE mta_send_internalId = @sndID
AND mta_sendMeta_name = @name)
	BEGIN
		UPDATE man_mta_sendMeta
		SET mta_sendMeta_value = @value
		WHERE mta_send_internalId = @sndID
		AND mta_sendMeta_name = @name
	END
ELSE
	BEGIN
		INSERT INTO man_mta_sendMeta(mta_send_internalId, mta_sendMeta_name, mta_sendMeta_value)
		VALUES(@sndID, @name, @value)
	END";
				cmd.Parameters.AddWithValue("@sndID", internalSendID);
				cmd.Parameters.AddWithValue("@name", metadata.Name);
				cmd.Parameters.AddWithValue("@value", metadata.Value);
				conn.Open();
				cmd.ExecuteNonQuery();
			}

			return true;
		}
	}
}
