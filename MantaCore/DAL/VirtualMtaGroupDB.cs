using System.Data;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class VirtualMtaGroupDB
	{
		/// <summary>
		/// Gets a Virtual MTA Group from the database; doesn't include Virtual MTA objects.
		/// </summary>
		/// <param name="ID"></param>
		/// <returns></returns>
		internal static VirtualMta.VirtualMtaGroup GetVirtualMtaGroup(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_group as [grp]
WHERE [grp].ip_group_id = @groupID";
				cmd.Parameters.AddWithValue("@groupID", id);
				return DataRetrieval.GetSingleObjectFromDatabase<VirtualMta.VirtualMtaGroup>(cmd, CreateAndFillVirtualMtaGroup);
			}
		}

		/// <summary>
		/// Gets all of the Virtual MTA Groups from the database; doesn't include Virtual MTA objects.
		/// </summary>
		/// <returns></returns>
		internal static VirtualMta.VirtualMtaGroupCollection GetVirtualMtaGroups()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_group";
				return new VirtualMta.VirtualMtaGroupCollection(DataRetrieval.GetCollectionFromDatabase<VirtualMta.VirtualMtaGroup>(cmd, CreateAndFillVirtualMtaGroup));
			}
		}

		/// <summary>
		/// Creates a MtaIPGroup object using the Data Record.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		private static VirtualMta.VirtualMtaGroup CreateAndFillVirtualMtaGroup(IDataRecord record)
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
