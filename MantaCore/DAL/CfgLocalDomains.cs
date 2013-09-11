using System.Data;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class CfgLocalDomains
	{
		/// <summary>
		/// Gets an array of the local domains from the database.
		/// All domains are toLowered!
		/// </summary>
		/// <returns></returns>
		public static LocalDomainCollection GetLocalDomainsArray()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_cfg_localDomain";
				return new LocalDomainCollection(DataRetrieval.GetCollectionFromDatabase<LocalDomain>(cmd, CreateAndFillLocalDomainFromRecord));
			}
		}

		/// <summary>
		/// Deletes all of the local domains from the database.
		/// </summary>
		public static void ClearLocalDomains()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"DELETE FROM man_cfg_localDomain";
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Saves a local domain to the database.
		/// </summary>
		/// <param name="domain">Domain to add. Does nothing if domain already exists.</param>
		public static void Save(LocalDomain localDomain)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM man_cfg_localDomain WHERE cfg_localDomain_id = @id)
	UPDATE man_cfg_localDomain
	SET cfg_localDomain_domain = @domain,
	cfg_localDomain_name = @name,
	cfg_localDomain_description = @description
	WHERE cfg_localDomain_domain = @id
ELSE
	BEGIN
	IF(@id > 0)
		BEGIN
			SET IDENTITY_INSERT man_cfg_localDomain ON

			INSERT INTO man_cfg_localDomain (cfg_localDomain_id, cfg_localDomain_domain, cfg_localDomain_name, cfg_localDomain_description)
			VALUES(@id, @domain, @name, @description)

			SET IDENTITY_INSERT man_cfg_localDomain OFF
		END
	ELSE
		INSERT INTO man_cfg_localDomain (cfg_localDomain_domain, cfg_localDomain_name, cfg_localDomain_description)
		VALUES(@domain, @name, @description)

	END";
				cmd.Parameters.AddWithValue("@id", localDomain.ID);
				cmd.Parameters.AddWithValue("@domain", localDomain.Hostname);
				cmd.Parameters.AddWithValue("@name", localDomain.Name);
				cmd.Parameters.AddWithValue("@description", localDomain.Description);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Creates a LocalDomain object and fills it with data from the data record.
		/// </summary>
		/// <param name="record">Record to get the data from.</param>
		/// <returns>LocalDomain object filled from record.</returns>
		private static LocalDomain CreateAndFillLocalDomainFromRecord(IDataRecord record)
		{
			return new LocalDomain
			{
				Description = record.GetStringOrEmpty("cfg_localDomain_description"),
				ID = record.GetInt32("cfg_localDomain_id"),
				Name = record.GetStringOrEmpty("cfg_localDomain_name"),
				Hostname = record.GetString("cfg_localDomain_domain")
			};
		}
	}
}
