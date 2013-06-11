using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Colony101.MTA.Library.MtaIpAddress;

namespace Colony101.MTA.Library.DAL
{
	public static class MtaIpAddressDB
	{
		/// <summary>
		/// Gets all of the MTA IP Addresses from the Database.
		/// </summary>
		/// <returns></returns>
		public static MtaIpAddressCollection GetMtaIpAddresses()
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM c101_ip_ipAddress";
				return new MtaIpAddressCollection(DataRetrieval.GetCollectionFromDatabase<MtaIpAddress.MtaIpAddress>(cmd, CreateAndFillMtaIpAddressFromRecord));
			}
		}

		/// <summary>
		/// Gets a collection of the MTA IP Addresses that belong to a MTA IP Group from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		internal static MtaIpAddress.MtaIpAddressCollection GetMtaIpGroupIps(int id)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT *
FROM c101_ip_ipAddress as [ip]
WHERE [ip].ip_ipAddress_id IN (SELECT [grp].ip_ipAddress_id FROM c101_ip_groupMembership as [grp] WHERE [grp].ip_group_id = @groupID) ";
				cmd.Parameters.AddWithValue("@groupID", id);
				return new MtaIpAddressCollection(DataRetrieval.GetCollectionFromDatabase<MtaIpAddress.MtaIpAddress>(cmd, CreateAndFillMtaIpAddressFromRecord));
			}
		}

		/// <summary>
		/// Creates a MtaIpAddress object filled with the values from the DataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static MtaIpAddress.MtaIpAddress CreateAndFillMtaIpAddressFromRecord(IDataRecord record)
		{
			MtaIpAddress.MtaIpAddress ipAddress = new MtaIpAddress.MtaIpAddress();
			ipAddress.ID = record.GetInt32("ip_ipAddress_id");
			ipAddress.Hostname = record.GetString("ip_ipAddress_hostname");
			ipAddress.IPAddress = System.Net.IPAddress.Parse(record.GetString("ip_ipAddress_ipAddress"));
			ipAddress.IsSmtpInbound = record.GetBoolean("ip_ipAddress_isInbound");
			ipAddress.IsSmtpOutbound = record.GetBoolean("ip_ipAddress_isOutbound");
			return ipAddress;
		}
	}
}
