using System;
using System.Collections;
using System.Configuration;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
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
		/// Gets the mimimum time between message send retries.
		/// </summary>
		/// <returns></returns>
		public static int GetRetryIntervalMinutes()
		{
			return (int)GetColumnValue("cfg_para_retryIntervalMinutes");
		}

		/// <summary>
		/// Gets the maximum time a message should be queued for from the DB.
		/// </summary>
		/// <returns></returns>
		public static int GetMaxTimeInQueueMinutes()
		{
			return (int)GetColumnValue("cfg_para_maxTimeInQueueMinutes");
		}

		/// <summary>
		/// Gets the ID of the default send group from the DB.
		/// </summary>
		/// <returns></returns>
		public static int GetDefaultIPGroupID()
		{
			return (int)GetColumnValue("cfg_para_defaultIpGroupId");
		}

		/// <summary>
		/// Gets the client connection idle timeout in seconds from the database.
		/// </summary>
		public static int GetClientIdleTimeout()
		{
			return (int)GetColumnValue("cfg_para_clientIdleTimeout");
		}

		/// <summary>
		/// Gets the connection receive timeout in seconds from the database.
		/// </summary>
		public static int GetReceiveTimeout()
		{
			return (int)GetColumnValue("cfg_para_receiveTimeout");
		}

		/// <summary>
		/// Gets the return path domain.
		/// </summary>
		public static string GetReturnPathDomain()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT [dmn].cfg_localDomain_domain
FROM man_cfg_localDomain as [dmn]
WHERE [dmn].cfg_localDomain_id = (SELECT TOP 1 [para].cfg_para_returnPathDomain_id FROM man_cfg_para as [para])";
				conn.Open();
				return cmd.ExecuteScalar().ToString();
			}
		}

		/// <summary>
		/// Gets the connection send timeout in seconds from the database.
		/// </summary>
		public static int GetSendTimeout()
		{
			return (int)GetColumnValue("cfg_para_sendTimeout");
		}

		/// <summary>
		/// ExecuteScalar getting value of colName in man_cfg_para
		/// </summary>
		/// <param name="colName"></param>
		/// <returns></returns>
		private static object GetColumnValue(string colName)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT " + colName + @"
FROM man_cfg_para";
				conn.Open();
				return cmd.ExecuteScalar();
			}
		}
	}
}
