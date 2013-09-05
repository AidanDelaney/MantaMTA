using System.Data;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class MtaIpGroupDB
	{
		/// <summary>
		/// Gets a MTA IP Group from the database; doesn't include IP Addresses.
		/// </summary>
		/// <param name="ID"></param>
		/// <returns></returns>
		internal static VirtualMta.VirtualMtaGroup GetMtaIpGroup(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_group as [grp]
WHERE [grp].ip_group_id = @groupID";
				cmd.Parameters.AddWithValue("@groupID", id);
				return DataRetrieval.GetSingleObjectFromDatabase<VirtualMta.VirtualMtaGroup>(cmd, CreateAndFillMtaIpGroup);
			}
		}

		/// <summary>
		/// Gets all of the MTA IP Groups from the database; doesn't include IP Addresses.
		/// </summary>
		/// <returns></returns>
		internal static VirtualMta.VirtualMtaGroupCollection GetMtaIpGroups()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_group";
				return new VirtualMta.VirtualMtaGroupCollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMtaGroup>(cmd, CreateAndFillMtaIpGroup));
			}
		}

		/// <summary>
		/// Creates a MtaIPGroup object using the Data Record.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static VirtualMta.VirtualMtaGroup CreateAndFillMtaIpGroup(IDataRecord record)
		{
			VirtualMta.VirtualMtaGroup group = new VirtualMta.VirtualMtaGroup();
			group.ID = record.GetInt32("ip_group_id");
			group.Name = record.GetString("ip_group_name");
			if(!record.IsDBNull("ip_group_description"))
				group.Description = record.GetString("ip_group_description");

			return group;
		}
	}
}
