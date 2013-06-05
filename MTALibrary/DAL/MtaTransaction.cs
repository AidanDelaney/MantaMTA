using System;
using System.Configuration;
using System.Data.SqlClient;
using Colony101.MTA.Library.Enums;

namespace Colony101.MTA.Library.DAL
{
	internal class MtaTransaction
	{
		/// <summary>
		/// Logs an MTA Transaction to the database.
		/// </summary>
		public static void LogTransaction(Guid msgID, TransactionStatus status, string svrResponse)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
INSERT INTO c101_mta_transaction (mta_msg_id, mta_transaction_timestamp, mta_transactionStatus_id, mta_transaction_serverResponse)
VALUES(@msgID, GETDATE(), @status, @serverResponse)";
				cmd.Parameters.AddWithValue("@msgID", msgID);
				cmd.Parameters.AddWithValue("@status", (int)status);
				cmd.Parameters.AddWithValue("@serverResponse", svrResponse);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}
	}
}
