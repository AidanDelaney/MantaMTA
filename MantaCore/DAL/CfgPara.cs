using System;
using System.Collections;
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
		/// Gets the base retry interval used for retry interval calculations.
		/// </summary>
		/// <returns></returns>
		public static int GetRetryIntervalBaseMinutes()
		{
			return (int)GetColumnValue("cfg_para_retryIntervalMinutes");
		}

		/// <summary>
		/// Sets the base message retry interval used for calculating the actual intervals.
		/// </summary>
		public static void SetRetryIntervalBaseMinutes(int minutes)
		{
			SetColumnValue("cfg_para_retryIntervalMinutes", minutes);
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
		/// Sets the maximum time a message should be queued for before it is timed out.
		/// </summary>
		/// <param name="minutes">Minutes to allow email to be queued for.</param>
		public static void SetMaxTimeInQueueMinutes(int minutes)
		{
			SetColumnValue("cfg_para_maxTimeInQueueMinutes", minutes);
		}

		/// <summary>
		/// Gets the ID of the default send group from the DB.
		/// </summary>
		/// <returns></returns>
		public static int GetDefaultVirtualMtaGroupID()
		{
			if(_DefaultVirtualMtaGroupID == null)
				_DefaultVirtualMtaGroupID = (int)GetColumnValue("cfg_para_defaultIpGroupId");
			return _DefaultVirtualMtaGroupID.Value;
		}

		public static int? _DefaultVirtualMtaGroupID = null;

		/// <summary>
		/// Sets the ID of the Virtual MTA Group sends should use by default.
		/// </summary>
		/// <param name="ID">ID of the Virtual MTA Group.</param>
		public static void SetDefaultVirtualMtaGroupID(int ID)
		{
			SetColumnValue("cfg_para_defaultIpGroupId", ID);
		}

		/// <summary>
		/// Gets the client connection idle timeout in seconds from the database.
		/// </summary>
		public static int GetClientIdleTimeout()
		{
			return (int)GetColumnValue("cfg_para_clientIdleTimeout");
		}

		/// <summary>
		/// Saves the client idle timeout value to the database.
		/// </summary>
		/// <param name="seconds"></param>
		public static void SetClientIdleTimeout(int seconds)
		{
			SetColumnValue("cfg_para_clientIdleTimeout", seconds);
		}

		/// <summary>
		/// Gets the amount of days to keep SMTP log files from the database.
		/// </summary>
		internal static int GetDaysToKeepSmtpLogsFor()
		{
			return (int)GetColumnValue("cfg_para_maxDaysToKeepSmtpLogs");
		}

		/// <summary>
		/// Sets the amount of days that SMTP logs should be kept for.
		/// </summary>
		/// <param name="days">Days to keep logs for.</param>
		internal static void SetDaysToKeepSmtpLogsFor(int days)
		{
			SetColumnValue("cfg_para_maxDaysToKeepSmtpLogs", days);
		}

		/// <summary>
		/// Gets the connection receive timeout in seconds from the database.
		/// </summary>
		public static int GetReceiveTimeout()
		{
			return (int)GetColumnValue("cfg_para_receiveTimeout");
		}

		/// <summary>
		/// Sets the connection receive timeout in the database.
		/// </summary>
		/// <param name="seconds">Seconds to wait before receive timeout exception is thrown.</param>
		public static void SetReceiveTimeout(int seconds)
		{
			SetColumnValue("cfg_para_receiveTimeout", seconds);
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
		/// Sets the default domain to use for ReturnPath host.
		/// </summary>
		/// <param name="localDomainID">ID of the localdomain.</param>
		public static void SetReturnPathLocalDomain(int localDomainID)
		{
			SetColumnValue("cfg_para_returnPathDomain_id", localDomainID);
		}

		/// <summary>
		/// Gets the connection send timeout in seconds from the database.
		/// </summary>
		public static int GetSendTimeout()
		{
			return (int)GetColumnValue("cfg_para_sendTimeout");
		}

		/// <summary>
		/// Saves the connection send timeout to the database.
		/// </summary>
		/// <param name="seconds">Seconds before the send timeout exception is thrown.</param>
		public static void SetSendTimeout(int seconds)
		{
			SetColumnValue("cfg_para_sendTimeout", seconds);
		}

		/// <summary>
		/// Gets the URL to post events to from the database.
		/// </summary>
		public static string GetEventForwardingHttpPostUrl()
		{
			return GetColumnValue("cfg_para_eventForwardingHttpPostUrl").ToString();
		}

		/// <summary>
		/// Sets the URL to post events to.
		/// </summary>
		/// <param name="url">URL to post to.</param>
		public static void SetEventForwardingHttpPostUrl(string url)
		{
			SetColumnValue("cfg_para_eventForwardingHttpPostUrl", url);
		}


		/// <summary>
		/// Gets the a flag from the database that indicates whether to keep or delete successfully processed bounce email files.
		/// Useful for Bounce Rule reviewing.
		/// </summary>
		public static bool GetKeepBounceFilesFlag()
		{
			return (bool)GetColumnValue("cfg_para_keepBounceFilesFlag");
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

		/// <summary>
		/// Saves the specified value to the column in the config table.
		/// </summary>
		/// <param name="colName">Name of the column to set.</param>
		/// <param name="value">Value to set.</param>
		public static void SetColumnValue(string colName, object value)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
UPDATE man_cfg_para
SET " + colName + @" = @value";
				conn.Open();
				cmd.Parameters.AddWithValue("@value", value);
				cmd.ExecuteNonQuery();
			}
		}
	}
}
