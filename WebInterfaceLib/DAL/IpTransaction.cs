using System.Configuration;
using System.Data.SqlClient;
using MantaMTA.Core.DAL;
using WebInterfaceLib.Model;

namespace WebInterfaceLib.DAL
{
	public static class IpTransaction
	{
		public static SendTransactionSummaryCollection GetSendSummaryForIpAddress(int ipAddressId)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT mta_transactionStatus_id, COUNT(*) AS 'Count'
FROM man_mta_transaction
WHERE ip_ipAddress_id = @ipAddressId
GROUP BY mta_transactionStatus_id";
				cmd.Parameters.AddWithValue("@ipAddressId", ipAddressId);
				return new SendTransactionSummaryCollection(DataRetrieval.GetCollectionFromDatabase<SendTransactionSummary>(cmd, CreateAndFillSendTransactionSummaryFromRecord));
			}
		}

		private static SendTransactionSummary CreateAndFillSendTransactionSummaryFromRecord(System.Data.IDataRecord record)
		{
			return new SendTransactionSummary((MantaMTA.Core.Enums.TransactionStatus)record.GetInt32("mta_transactionStatus_id"), record.GetInt32("count"));
		}
	}
}
