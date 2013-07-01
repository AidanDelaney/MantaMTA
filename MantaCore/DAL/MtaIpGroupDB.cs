using System.Configuration;
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
		public static MtaIpAddress.MtaIPGroup GetMtaIpGroup(int id)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_ip_group as [grp]
WHERE [grp].ip_group_id = @groupID";
				cmd.Parameters.AddWithValue("@groupID", id);
				return DataRetrieval.GetSingleObjectFromDatabase<MtaIpAddress.MtaIPGroup>(cmd, CreateAndFillMtaIpGroup);
			}
		}

		/// <summary>
		/// Creates a MtaIPGroup object using the Data Record.
		/// </summary>
		/// <param name="record"></param>
		/// <returns></returns>
		internal static MtaIpAddress.MtaIPGroup CreateAndFillMtaIpGroup(IDataRecord record)
		{
			MtaIpAddress.MtaIPGroup group = new MtaIpAddress.MtaIPGroup();
			group.ID = record.GetInt32("ip_group_id");
			group.Name = record.GetString("ip_group_name");
			if(!record.IsDBNull("ip_group_description"))
				group.Description = record.GetString("ip_group_description");

			return group;
		}
	}
}
