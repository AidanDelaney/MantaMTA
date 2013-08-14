using System.Collections;
using System.Configuration;
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
		public static string[] GetLocalDomainsArray()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT cfg_localDomain_domain
FROM man_cfg_localDomain";
				conn.Open();
				ArrayList results = new ArrayList();
				SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
					results.Add(reader.GetString("cfg_localDomain_domain").ToLower());

				return (string[])results.ToArray(typeof(string));
			}
		}
	}
}
