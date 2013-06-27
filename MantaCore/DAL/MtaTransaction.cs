using System;
using System.Configuration;
using System.Data.SqlClient;
using MantaMTA.Core.Enums;

namespace MantaMTA.Core.DAL
{
	internal class MtaTransaction
	{
		/// <summary>
		/// Logs an MTA Transaction to the database.
		/// </summary>
		public static void LogTransaction(Guid msgID, TransactionStatus status, string svrResponse, MtaIpAddress.MtaIpAddress ipAddress)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
INSERT INTO c101_mta_transaction (mta_msg_id, ip_ipAddress_id, mta_transaction_timestamp, mta_transactionStatus_id, mta_transaction_serverResponse)
VALUES(@msgID, @ipAddressID, GETDATE(), @status, @serverResponse)";
				cmd.Parameters.AddWithValue("@msgID", msgID);
				if (ipAddress != null)
					cmd.Parameters.AddWithValue("@ipAddressID", ipAddress.ID);
				else
					cmd.Parameters.AddWithValue("@ipAddressID", DBNull.Value);
				cmd.Parameters.AddWithValue("@status", (int)status);
				cmd.Parameters.AddWithValue("@serverResponse", svrResponse);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}
	}
}
