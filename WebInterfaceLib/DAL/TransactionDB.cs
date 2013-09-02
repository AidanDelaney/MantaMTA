using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.DAL;
using MantaMTA.Core.Enums;
using WebInterfaceLib.Model;

namespace WebInterfaceLib.DAL
{
	public static class TransactionDB
	{
		/// <summary>
		/// Gets information about the speed of a send.
		/// </summary>
		/// <param name="sendID">ID of the send to get speed information about.</param>
		/// <returns>SendSpeedInfo</returns>
		public static SendSpeedInfo GetSendSpeedInfo(string sendID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
DECLARE @internalSendID int
SELECT @internalSendID = mta_send_internalId
FROM man_mta_send
WHERE mta_send_id = @sndID

SELECT COUNT(*) AS 'Count', [tran].mta_transactionStatus_id, CONVERT(smalldatetime, [tran].mta_transaction_timestamp) as 'mta_transaction_timestamp'
FROM man_mta_transaction as [tran]
JOIN man_mta_msg AS [msg] ON [tran].mta_msg_id = [msg].mta_msg_id
WHERE [msg].mta_send_internalId = @internalSendID
GROUP BY [tran].mta_transactionStatus_id, CONVERT(smalldatetime, [tran].mta_transaction_timestamp)
ORDER BY CONVERT(smalldatetime, [tran].mta_transaction_timestamp)";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				return new SendSpeedInfo(DataRetrieval.GetCollectionFromDatabase<SendSpeedInfoItem>(cmd, CreateAndFillSendSpeedInfoItemFromRecord));
			}
		}

		/// <summary>
		/// Creates a SendSpeedInfoItem object and fills it with data from the data record.
		/// </summary>
		/// <param name="record">Contains the data to use for filling.</param>
		/// <returns>A SendSpeedInfoItem object filled with data from the record.</returns>
		private static SendSpeedInfoItem CreateAndFillSendSpeedInfoItemFromRecord(IDataRecord record)
		{
			SendSpeedInfoItem item = new SendSpeedInfoItem
			{
				Count = record.GetInt32("Count"),
				Status = (TransactionStatus)record.GetInt32("mta_transactionStatus_id"),
				Timestamp = record.GetDateTime("mta_transaction_timestamp")
			};
			return item;
		}

		/// <summary>
		/// Gets a data page about bounces from the transactions table for a send.
		/// </summary>
		/// <param name="sendID">Send to get data for.</param>
		/// <param name="pageNum">The page to get.</param>
		/// <param name="pageSize">The size of the data pages.</param>
		/// <returns>An array of BounceInfo from the data page.</returns>
		public static BounceInfo[] GetBounceInfo(string sendID, int pageNum, int pageSize)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
declare @internalSendID int
SELECT @internalSendID = mta_send_internalId
FROM man_mta_send
WHERE mta_send_id = @sndID

SELECT [sorted].*
FROM (
		SELECT ROW_NUMBER() OVER (ORDER BY count(*) DESC, mta_transaction_serverHostname) as 'Row', mta_transactionStatus_id, mta_transaction_serverResponse, mta_transaction_serverHostname as 'mta_transaction_serverHostname', [ip].ip_ipAddress_hostname, [ip].ip_ipAddress_ipAddress, COUNT(*) as 'Count'
		FROM man_mta_transaction as [tran]
		JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id
		JOIN man_ip_ipAddress as [ip] ON [tran].ip_ipAddress_id = [ip].ip_ipAddress_id
		WHERE [msg].mta_send_internalId = @internalSendID 
		AND mta_transactionStatus_id IN (1, 2, 3, 6)
		GROUP BY mta_transactionStatus_id, mta_transaction_serverResponse, mta_transaction_serverHostname,[ip].ip_ipAddress_hostname, [ip].ip_ipAddress_ipAddress
	 ) as [sorted]
WHERE [Row] >= " + (((pageNum * pageSize) - pageSize) + 1) + " AND [Row] <= " + (pageNum * pageSize);
				cmd.Parameters.AddWithValue("@sndID", sendID);
				return DataRetrieval.GetCollectionFromDatabase<BounceInfo>(cmd, CreateAndFillBounceInfo).ToArray();
			}
		}

		/// <summary>
		/// Counts the total amount of bounces for a send.
		/// </summary>
		/// <param name="sendID">ID of the send to count bounces for.</param>
		/// <returns>The amount of bounces for the send.</returns>
		public static int GetBounceCount(string sendID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
declare @internalSendID int
SELECT @internalSendID = mta_send_internalId
FROM man_mta_send
WHERE mta_send_id = @sndID

SELECT COUNT(*)
FROM(
SELECT 1 as 'Col'
		FROM man_mta_transaction as [tran]
		JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id
		JOIN man_ip_ipAddress as [ip] ON [tran].ip_ipAddress_id = [ip].ip_ipAddress_id
		WHERE [msg].mta_send_internalId = @internalSendID 
		AND mta_transactionStatus_id IN (1, 2, 3, 6)
	GROUP BY mta_transactionStatus_id, mta_transaction_serverResponse, mta_transaction_serverHostname,[ip].ip_ipAddress_hostname, [ip].ip_ipAddress_ipAddress
	) as [sorted]";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				conn.Open();
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}

		/// <summary>
		/// Creates a BounceInfo object and fills it with data from the data record.
		/// </summary>
		/// <param name="record">Where to get the data from.</param>
		/// <returns>BounceInfo filled with data from the data record.</returns>
		private static BounceInfo CreateAndFillBounceInfo(IDataRecord record)
		{
			BounceInfo bounceInfo = new BounceInfo();
			bounceInfo.Count = record.GetInt32("Count");
			bounceInfo.LocalHostname = record.GetString("ip_ipAddress_hostname");
			bounceInfo.LocalIpAddress = record.GetString("ip_ipAddress_ipAddress");
			bounceInfo.Message = record.GetString("mta_transaction_serverResponse");
			bounceInfo.RemoteHostname = record.GetStringOrEmpty("mta_transaction_serverHostname");
			bounceInfo.TransactionStatus = (TransactionStatus)record.GetInt32("mta_transactionStatus_id");
			return bounceInfo;
		}
	}
}
