using MantaMTA.Core.Enums;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MantaMTA.Core.DAL
{
	internal class MtaTransaction
	{
		/// <summary>
		/// Logs an MTA Transaction to the database.
		/// </summary>
		public static void LogTransaction(Guid msgID, TransactionStatus status, string svrResponse, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			LogTransactionAsync(msgID, status, svrResponse, ipAddress, mxRecord).Wait();
		}

		/// <summary>
		/// Logs an MTA Transaction to the database.
		/// </summary>
		public static async Task<bool> LogTransactionAsync(Guid msgID, TransactionStatus status, string svrResponse, VirtualMta.VirtualMTA ipAddress, DNS.MXRecord mxRecord)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
INSERT INTO man_mta_transaction (mta_msg_id, ip_ipAddress_id, mta_transaction_timestamp, mta_transactionStatus_id, mta_transaction_serverResponse, mta_transaction_serverHostname)
VALUES(@msgID, @ipAddressID, GETUTCDATE(), @status, @serverResponse, @serverHostname)";
				cmd.Parameters.AddWithValue("@msgID", msgID);
				if (ipAddress != null)
					cmd.Parameters.AddWithValue("@ipAddressID", ipAddress.ID);
				else
					cmd.Parameters.AddWithValue("@ipAddressID", DBNull.Value);

				if (mxRecord != null)
					cmd.Parameters.AddWithValue("@serverHostname", mxRecord.Host);
				else
					cmd.Parameters.AddWithValue("@serverHostname", DBNull.Value);

				cmd.Parameters.AddWithValue("@status", (int)status);
				cmd.Parameters.AddWithValue("@serverResponse", svrResponse);
				await conn.OpenAsync();
				await cmd.ExecuteNonQueryAsync();
				return true;
			}
		}
	}
}
