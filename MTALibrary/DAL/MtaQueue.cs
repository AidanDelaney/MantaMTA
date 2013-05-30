using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Colony101.MTA.Library.DAL
{
	internal class MtaQueueDB
	{
		/// <summary>
		/// Insert into the outbound queue.
		/// </summary>
		/// <param name="outboundIP"></param>
		/// <param name="messageID"></param>
		/// <param name="mailFrom"></param>
		/// <param name="rcptTo"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		internal static bool Insert(string outboundIP, Guid messageID, string mailFrom, string rcptTo, string data)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
INSERT INTO c101_mta_queue (mta_queue_outboundIP, mta_queue_msgID, mta_queue_queuedTimestamp, mta_queue_lastAttemptTimestamp, mta_queue_mailFrom, mta_queue_rcptTo, mta_queue_data,	mta_queue_isMtaLocked)
VALUES(@outboundIP, @msgID,  GETDATE(), NULL, @mailFrom, @rcptTo, @data, 0)";
				cmd.Parameters.AddWithValue("@outboundIP", outboundIP);
				cmd.Parameters.AddWithValue("@msgID", messageID);
				cmd.Parameters.AddWithValue("@mailFrom", mailFrom);
				cmd.Parameters.AddWithValue("@rcptTo", rcptTo);
				cmd.Parameters.AddWithValue("@data", data);
				conn.Open();
				return cmd.ExecuteNonQuery() == 1;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		internal static MtaQueueItem PickupAndLockQueueItems(int count)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
INSERT INTO @msgIdTbl
SELECT TOP (@maxItems) mta_queue_msgID
FROM c101_mta_queue
ORDER BY mta_queue_queuedTimestamp ASC

UPDATE c101_mta_queue 
SET mta_queue_isMtaLocked = 'true'
WHERE mta_queue_msgID IN (SELECT * FROM @msgIdTbl)

SELECT *
FROM c101_mta_queue
WHERE mta_queue_msgID IN (SELECT * FROM @msgIdTbl)
";
				cmd.Parameters.AddWithValue("@maxItems", count);
				throw new Exception("Not implemented");
			}
		}
	}

	internal class MtaQueueItem
	{
		public string OutboundIP { get; set; }
		public Guid MessageID { get; set; }
		public DateTime QueuedTimestamp { get; set; }
		public DateTime LastAttemptTimestamp { get; set; }

		public string MailFrom { get; set; }
		public string RcptTo { get; set; }
		public string Data { get; set; }
	}
}
