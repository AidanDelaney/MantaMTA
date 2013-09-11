using System.Collections;
using System.Data.SqlClient;
using System.Net;

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
			using (SqlConnection conn = MantaDB.GetSqlConnection())
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

		/// <summary>
		/// Saves the array of IP Address that are allowed to relay messages through MantaMTA.
		/// Overwrites the existing addresses.
		/// </summary>
		/// <param name="addresses">IP Addresses to allow relaying for.</param>
		public static void SetRelayingPermittedIPAddresses(IPAddress[] addresses)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"DELETE FROM man_cfg_relayingPermittedIp";
				foreach (IPAddress addr in addresses)
				{
					cmd.CommandText += System.Environment.NewLine + "INSERT INTO man_cfg_relayingPermittedIp(cfg_relayingPermittedIp_ip) VALUES ( '" + addr.ToString() + "' )";
				}
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}
	}
}
