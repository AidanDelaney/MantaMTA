using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;
using WebInterfaceLib.BO;

namespace WebInterfaceLib.DAL
{
	public static class SendDB
	{
		/// <summary>
		/// Gets the amount of messages currently waiting in the queue for sending.
		/// </summary>
		/// <returns>Count of the messages waiting in the queue.</returns>
		public static int GetWaitingCount()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT COUNT(*)
FROM man_mta_queue";
				conn.Open();
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}

		/// <summary>
		/// Get a count of all the sends in the MantaMTA database.
		/// </summary>
		/// <returns>Count of all Sends.</returns>
		public static int GetSendsCount()
		{
			using (SqlConnection conn = MantaMTA.Core.DAL.MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT COUNT(*) FROM man_mta_send";
				conn.Open();
				return Convert.ToInt32(cmd.ExecuteScalar());
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
				cmd.CommandText = @"SELECT [sorted].*,
	(SELECT COUNT(*) FROM man_mta_msg WHERE man_mta_msg.mta_send_internalId = [sorted].mta_send_internalId) AS 'Messages',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId AND [tran].mta_transactionStatus_id = 4) AS 'Accepted',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId AND ([tran].mta_transactionStatus_id = 2 OR [tran].mta_transactionStatus_id = 3 OR [tran].mta_transactionStatus_id = 6)) AS 'Rejected',
	(SELECT COUNT(*) FROM man_mta_queue as [queue] JOIN man_mta_msg as [msg] ON [queue].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId) AS 'Waiting',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred',
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] JOIN  man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = [sorted].mta_send_internalId) AS 'LastTransactionTimestamp'
FROM (SELECT *, ROW_NUMBER() OVER(ORDER BY mta_send_createdTimestamp DESC) as 'RowNum'
FROM man_mta_send) as [sorted]
WHERE RowNum >= " + ((pageNum * pageSize) - pageSize + 1) + " AND RowNum <= " + (pageSize * pageNum) + @"
ORDER BY RowNum ASC";
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
				cmd.CommandText = @"SELECT [sorted].*
FROM (SELECT *, ROW_NUMBER() OVER(ORDER BY mta_send_createdTimestamp DESC) as 'RowNum',
	(SELECT COUNT(*) FROM man_mta_msg WHERE man_mta_msg.mta_send_internalId = man_mta_send.mta_send_internalId) AS 'Messages',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId AND [tran].mta_transactionStatus_id = 4) AS 'Accepted',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId AND ([tran].mta_transactionStatus_id = 2 OR [tran].mta_transactionStatus_id = 3 OR [tran].mta_transactionStatus_id = 6)) AS 'Rejected',
	(SELECT COUNT(*) FROM man_mta_queue as [queue] JOIN man_mta_msg as [msg] ON [queue].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId) AS 'Waiting',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred',
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] JOIN  man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [msg].mta_send_internalId = man_mta_send.mta_send_internalId) AS 'LastTransactionTimestamp'
FROM man_mta_send) as [sorted]
WHERE [sorted].Waiting > 0
ORDER BY RowNum ASC";
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
				SendStatus = (SendStatus)record.GetInt32("mta_sendStatus_id"),
				LastAccessedTimestamp = DateTime.UtcNow,
				CreatedTimestamp = record.GetDateTime("mta_send_createdTimestamp"),
				Accepted = record.GetInt32("Accepted"),
				Deferred = record.GetInt32("Deferred"),
				Rejected = record.GetInt32("Rejected"),
				Throttled = record.GetInt32("Throttled"),
				TotalMessages = record.GetInt32("Messages"),
				Waiting = record.GetInt32("Waiting")
			};

			if (!record.IsDBNull("LastTransactionTimestamp"))
				sInfo.LastTransactionTimestamp = record.GetDateTime("LastTransactionTimestamp");

			return sInfo;
		}
	}
}
