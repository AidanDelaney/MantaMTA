using System;
using System.Collections;
using System.Configuration;
using System.Data.SqlClient;

namespace Colony101.MTA.Library.DAL
{
	internal static class CfgPara
	{
		/// <summary>
		/// Gets the IP Addresses that SMTP servers should listen for client on from the database.
		/// </summary>
		/// <returns></returns>
		public static int[] GetServerListenPorts()
		{
			string[] results = GetColumnValue("cfg_para_listenPorts").ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			ArrayList toReturn = new ArrayList();
			for (int i = 0; i < results.Length; i++)
				toReturn.Add(Int32.Parse(results[i]));

			return (int[])toReturn.ToArray(typeof(int));
		}

		/// <summary>
		/// Gets the path to the drop folder.
		/// </summary>
		/// <returns></returns>
		public static string GetDropFolder()
		{
			return GetColumnValue("cfg_para_dropFolder").ToString();
		}

		/// <summary>
		/// Gets the path to the queue folder.
		/// </summary>
		/// <returns></returns>
		public static string GetQueueFolder()
		{
			return GetColumnValue("cfg_para_queueFolder").ToString();
		}

		/// <summary>
		/// Gets the path to the log folder.
		/// </summary>
		/// <returns></returns>
		public static string GetLogFolder()
		{
			return GetColumnValue("cfg_para_logFolder").ToString();
		}

		/// <summary>
		/// ExecuteScalar getting value of colName in c101_cfg_para
		/// </summary>
		/// <param name="colName"></param>
		/// <returns></returns>
		private static object GetColumnValue(string colName)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT " + colName + @"
FROM c101_cfg_para";
				conn.Open();
				return cmd.ExecuteScalar();
			}
		}
	}
}
