using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.VirtualMta;

namespace MantaMTA.Core.DAL
{
	public static class MtaIpAddressDB
	{
		/// <summary>
		/// Gets all of the MTA IP Addresses from the Database.
		/// </summary>
		/// <returns></returns>
		public static VirtualMTACollection GetMtaIpAddresses()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_ipAddress";
				return new VirtualMTACollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillMtaIpAddressFromRecord));
			}
		}

		/// <summary>
		/// Gets a single MTA IP Addresses from the Database.
		/// </summary>
		/// <returns></returns>
		public static VirtualMta.VirtualMTA GetMtaIpAddress(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_ipAddress
WHERE ip_ipAddress_id = @id";
				cmd.Parameters.AddWithValue("@id", id);
				return DataRetrieval.GetSingleObjectFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillMtaIpAddressFromRecord);
			}
		}

		/// <summary>
		/// Gets a collection of the MTA IP Addresses that belong to a MTA IP Group from the database.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		internal static VirtualMta.VirtualMTACollection GetMtaIpGroupIps(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT *
FROM man_ip_ipAddress as [ip]
WHERE [ip].ip_ipAddress_id IN (SELECT [grp].ip_ipAddress_id FROM man_ip_groupMembership as [grp] WHERE [grp].ip_group_id = @groupID) ";
				cmd.Parameters.AddWithValue("@groupID", id);
				return new VirtualMTACollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillMtaIpAddressFromRecord));
			}
		}

		/// <summary>
		/// Creates a MtaIpAddress object filled with the values from the DataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static VirtualMta.VirtualMTA CreateAndFillMtaIpAddressFromRecord(IDataRecord record)
		{
			VirtualMta.VirtualMTA ipAddress = new VirtualMta.VirtualMTA();
			ipAddress.ID = record.GetInt32("ip_ipAddress_id");
			ipAddress.Hostname = record.GetString("ip_ipAddress_hostname");
			ipAddress.IPAddress = System.Net.IPAddress.Parse(record.GetString("ip_ipAddress_ipAddress"));
			ipAddress.IsSmtpInbound = record.GetBoolean("ip_ipAddress_isInbound");
			ipAddress.IsSmtpOutbound = record.GetBoolean("ip_ipAddress_isOutbound");
			return ipAddress;
		}
	}
}
