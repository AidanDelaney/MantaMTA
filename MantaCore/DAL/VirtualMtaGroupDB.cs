using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

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

		/// <summary>
		/// Saves the virtual mta group to the database.
		/// </summary>
		/// <param name="grp">Group to save.</param>
		internal static void Save(VirtualMta.VirtualMtaGroup grp)
		{
			StringBuilder groupMembershipInserts = new StringBuilder();
			foreach(VirtualMta.VirtualMTA vmta in grp.VirtualMtaCollection)
				groupMembershipInserts.AppendFormat(@"{1}INSERT INTO man_ip_groupMembership(ip_group_id, ip_ipAddress_id)
VALUES(@id,{0}){1}", vmta.ID, Environment.NewLine);

			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
BEGIN TRANSACTION

IF EXISTS(SELECT 1 FROM man_ip_group WHERE ip_group_id = @id)
	UPDATE man_ip_group
	SET ip_group_name = @name,
		ip_group_description = @description
	WHERE ip_group_id = @id
ELSE
	BEGIN
		INSERT INTO man_ip_group(ip_group_name, ip_group_description)
		VALUES(@name, @description)

		SELECT @id = @@IDENTITY
	END

DELETE 
FROM man_ip_groupMembership
WHERE ip_group_id = @id

" + groupMembershipInserts.ToString() + @"

COMMIT TRANSACTION";
				cmd.Parameters.AddWithValue("@id", grp.ID);
				cmd.Parameters.AddWithValue("@name", grp.Name);
				cmd.Parameters.AddWithValue("@description", grp.Description);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Deletes the specified Virtual MTA group.
		/// </summary>
		/// <param name="id">ID of the virtual mta group to delete.</param>
		internal static void Delete(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
DELETE 
FROM man_ip_group
WHERE ip_group_id = @id

DELETE 
FROM man_ip_groupMembership
WHERE ip_group_id = @id";
				cmd.Parameters.AddWithValue("@id", id);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}
	}
}
