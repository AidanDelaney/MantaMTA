using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebInterface.Controllers
{
    public class SendsController : Controller
    {
        //
        // GET: /Sends/

        public ActionResult Index()
        {
			return View(GetSendListDataSet(false));
        }

		public ActionResult Active()
		{
			return View(GetSendListDataSet(true));
		}

		
		public ActionResult Report(string sendID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT [msg].mta_msg_rcptTo,
[tran].*,
[ip].ip_ipAddress_ipAddress,
[ip].ip_ipAddress_hostname
FROM c101_mta_transaction as [tran]
JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id
JOIN c101_mta_send as [snd] on [snd].mta_send_internalId = [msg].mta_send_internalId
LEFT JOIN c101_ip_ipAddress as [ip] on [tran].ip_ipAddress_id = [ip].ip_ipAddress_id
WHERE mta_transactionStatus_id < 4
AND [snd].mta_send_id = @sndID
ORDER BY [tran].mta_transaction_timestamp DESC";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				conn.Open();
				DataSet[] results = new DataSet[] { new DataSet(), new DataSet() };
				SqlDataAdapter da = new SqlDataAdapter(cmd);
				da.Fill(results[0]);

				cmd.CommandText = @"
SELECT *
FROM
(select 
	CONVERT(varchar, DATEPART(YEAR, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(MONTH, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(DAY, [tran].mta_transaction_timestamp)) + ' ' +
	CONVERT(varchar, DATEPART(HOUR, [tran].mta_transaction_timestamp)) + ':' +
	CONVERT(varchar, DATEPART(MINUTE, [tran].mta_transaction_timestamp)) as 'Date', 
	count(*) as 'Sent',
	4 as 'status'
from c101_mta_transaction as [tran]
join c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id
join c101_mta_send as [snd] on [msg].mta_send_internalId = [snd].mta_send_internalId
where mta_transactionStatus_id = 4
and [snd].mta_send_id = @sndID
GROUP BY DATEPART(YEAR, [tran].mta_transaction_timestamp), DATEPART(MONTH, [tran].mta_transaction_timestamp), DATEPART(DAY, [tran].mta_transaction_timestamp), DATEPART(HOUR, [tran].mta_transaction_timestamp), DATEPART(MINUTE, [tran].mta_transaction_timestamp)) sent
UNION
(select 
	CONVERT(varchar, DATEPART(YEAR, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(MONTH, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(DAY, [tran].mta_transaction_timestamp)) + ' ' +
	CONVERT(varchar, DATEPART(HOUR, [tran].mta_transaction_timestamp)) + ':' +
	CONVERT(varchar, DATEPART(MINUTE, [tran].mta_transaction_timestamp)) as 'Date', 
	count(*) as 'Deferred',
	2 as 'Status'
from c101_mta_transaction as [tran]
join c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id
join c101_mta_send as [snd] on [msg].mta_send_internalId = [snd].mta_send_internalId
where mta_transactionStatus_id = 2
and [snd].mta_send_id = @sndID
GROUP BY DATEPART(YEAR, [tran].mta_transaction_timestamp), DATEPART(MONTH, [tran].mta_transaction_timestamp), DATEPART(DAY, [tran].mta_transaction_timestamp), DATEPART(HOUR, [tran].mta_transaction_timestamp), DATEPART(MINUTE, [tran].mta_transaction_timestamp))
UNION (select 
	CONVERT(varchar, DATEPART(YEAR, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(MONTH, [tran].mta_transaction_timestamp)) + '-' +
	CONVERT(varchar, DATEPART(DAY, [tran].mta_transaction_timestamp)) + ' ' +
	CONVERT(varchar, DATEPART(HOUR, [tran].mta_transaction_timestamp)) + ':' +
	CONVERT(varchar, DATEPART(MINUTE, [tran].mta_transaction_timestamp)) as 'Date', 
	count(*) as 'Failed',
	1 as 'Status'
from c101_mta_transaction as [tran]
join c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id
join c101_mta_send as [snd] on [msg].mta_send_internalId = [snd].mta_send_internalId
where mta_transactionStatus_id = 1
and [snd].mta_send_id = @sndID
GROUP BY DATEPART(YEAR, [tran].mta_transaction_timestamp), DATEPART(MONTH, [tran].mta_transaction_timestamp), DATEPART(DAY, [tran].mta_transaction_timestamp), DATEPART(HOUR, [tran].mta_transaction_timestamp), DATEPART(MINUTE, [tran].mta_transaction_timestamp))
ORDER BY [Date] ASC
";
				da.Fill(results[1]);

				return View(results);
			}
		}

		private DataSet GetSendListDataSet(bool onlyActive)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT [snd].mta_send_id as 'SendID',
[snd].mta_send_createdTimestamp as 'Started',
(SELECT COUNT(*) FROM c101_mta_msg as [msg] WHERE [msg].mta_send_internalId = [snd].mta_send_internalId) as 'Messages',
(SELECT COUNT(*) FROM c101_mta_queue as [queue] 
	JOIN c101_mta_msg as [msg] on [queue].mta_msg_id = [msg].mta_msg_id
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId) as 'Waiting',
(SELECT COUNT(*) FROM c101_mta_transaction as [tran]
	JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id 
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId
	AND [tran].mta_transactionStatus_id = 1) as 'Deferred',
(SELECT COUNT(*) FROM c101_mta_transaction as [tran]
	JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id 
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId
	AND [tran].mta_transactionStatus_id = 2) as 'Failed',
(SELECT COUNT(*) FROM c101_mta_transaction as [tran]
	JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id 
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId
	AND [tran].mta_transactionStatus_id = 3) as 'Timed Out',
(SELECT COUNT(*) FROM c101_mta_transaction as [tran]
	JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id 
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId
	AND [tran].mta_transactionStatus_id = 3) as 'Throttled',
(SELECT COUNT(*) FROM c101_mta_transaction as [tran]
	JOIN c101_mta_msg as [msg] on [tran].mta_msg_id = [msg].mta_msg_id 
	WHERE [msg].mta_send_internalId = [snd].mta_send_internalId
	AND [tran].mta_transactionStatus_id = 4) as 'Delivered'
FROM c101_mta_send as [snd]"
  + (onlyActive ? " WHERE [snd].mta_send_internalId IN (SELECT c101_mta_msg.mta_send_internalId FROM c101_mta_queue join c101_mta_msg on c101_mta_queue.mta_msg_id = c101_mta_msg.mta_msg_id) " : string.Empty)
  +	" ORDER BY [snd].mta_send_createdTimestamp DESC";

				conn.Open();
				DataSet results = new DataSet();
				SqlDataAdapter da = new SqlDataAdapter(cmd);
				da.Fill(results);

				return results;
			}
		}
    }
}
