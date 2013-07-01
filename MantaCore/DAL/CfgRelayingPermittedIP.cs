using System.Collections;
using System.Configuration;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class CfgRelayingPermittedIP
	{
		/// <summary>
		/// Gets an array of the IP addresses that are permitted to use this server for relaying from the database.
		/// </summary>
		/// <returns></returns>
		public static string[] GetRelayingPermittedIPAddresses()
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT cfg_relayingPermittedIp_ip
FROM man_cfg_relayingPermittedIp";
				conn.Open();
				ArrayList results = new ArrayList();
				SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
					results.Add(reader.GetString("cfg_relayingPermittedIp_ip"));

				return (string[])results.ToArray(typeof(string));
			}
		}
	}
}
