using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.VirtualMta;

namespace MantaMTA.Core.DAL
{
	public static class VirtualMtaDB
	{
		/// <summary>
		/// Gets all of the MTA IP Addresses from the Database.
		/// </summary>
		/// <returns></returns>
		public static VirtualMTACollection GetVirtualMtas()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_ipAddress";
				return new VirtualMTACollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillVirtualMtaFromRecord));
			}
		}

		/// <summary>
		/// Gets a single MTA IP Addresses from the Database.
		/// </summary>
		/// <returns></returns>
		public static VirtualMta.VirtualMTA GetVirtualMta(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_ipAddress
WHERE ip_ipAddress_id = @id";
				cmd.Parameters.AddWithValue("@id", id);
				return DataRetrieval.GetSingleObjectFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillVirtualMtaFromRecord);
			}
		}

		/// <summary>
		/// Gets a collection of the Virtual MTAs that belong to a Virtual MTA Group from the database.
		/// </summary>
		/// <param name="groupID">ID of the Virtual MTA Group to get Virtual MTAs for.</param>
		/// <returns></returns>
		internal static VirtualMta.VirtualMTACollection GetVirtualMtasInVirtualMtaGroup(int groupID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT *
FROM man_ip_ipAddress as [ip]
WHERE [ip].ip_ipAddress_id IN (SELECT [grp].ip_ipAddress_id FROM man_ip_groupMembership as [grp] WHERE [grp].ip_group_id = @groupID) ";
				cmd.Parameters.AddWithValue("@groupID", groupID);
				return new VirtualMTACollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMTA>(cmd, CreateAndFillVirtualMtaFromRecord));
			}
		}

		/// <summary>
		/// Creates a VirtualMTA object filled with the values from the DataRecord.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static VirtualMta.VirtualMTA CreateAndFillVirtualMtaFromRecord(IDataRecord record)
		{
			VirtualMta.VirtualMTA vmta = new VirtualMta.VirtualMTA();
			vmta.ID = record.GetInt32("ip_ipAddress_id");
			vmta.Hostname = record.GetString("ip_ipAddress_hostname");
			vmta.IPAddress = System.Net.IPAddress.Parse(record.GetString("ip_ipAddress_ipAddress"));
			vmta.IsSmtpInbound = record.GetBoolean("ip_ipAddress_isInbound");
			vmta.IsSmtpOutbound = record.GetBoolean("ip_ipAddress_isOutbound");
			return vmta;
		}
	}
}
