using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using MantaMTA.Core.ServiceContracts;
using WebInterface.Models;
using WebInterfaceLib.BO;
using MantaMTA.Core.Sends;

namespace WebInterface.Controllers
{
    public class SendsController : Controller
    {
        //
        // GET: /Sends/
        public ActionResult Index(int page = 1, int pageSize = 10)
        {
			SendInfoCollection sends = WebInterfaceLib.DAL.SendDB.GetSends(pageSize, page);
			int pages = (int)Math.Ceiling(WebInterfaceLib.DAL.SendDB.GetSendsCount() / Convert.ToDouble(pageSize));
			return View(new SendsModel(sends, page, pages));
        }

		//
		// GET: /Queue/
		public ActionResult Queue()
		{
			SendInfoCollection sends = WebInterfaceLib.DAL.SendDB.GetSendsInProgress();
			return View(new SendsModel(sends, 1, 1));
		}

		//
		// GET: /Sends/Overview?sendID=
		public ActionResult Overview(string sendID)
		{
			SendInfo send = WebInterfaceLib.DAL.SendDB.GetSend(sendID);
			return View(new SendReportOverview(send));
		}

		//
		// GET: /Sends/VirtualMTA?sendID=
		public ActionResult VirtualMTA(string sendID)
		{
			return View(new SendReportVirtualMta(WebInterfaceLib.DAL.VirtualMtaDB.GetSendVirtualMTAStats(sendID), sendID));
		}

		//
		// GET: /Sends/Bounces?sendID=
		public ActionResult Bounces(string sendID, int page = 1, int pageSize = 25)
		{
			int bounceCount = WebInterfaceLib.DAL.TransactionDB.GetBounceCount(sendID);
			int pageCount = (int)Math.Ceiling(bounceCount / Convert.ToDouble(pageSize));
			return View(new SendReportBounces(WebInterfaceLib.DAL.TransactionDB.GetBounceInfo(sendID, page, pageSize), sendID, page, pageCount));
		}

		//
		// GET: /Sends/Deferred?sendID=
		public ActionResult Deferred(string sendID, int page = 1, int pageSize = 25)
		{
			int bounceCount = WebInterfaceLib.DAL.TransactionDB.GetDeferredCount(sendID);
			int pageCount = (int)Math.Ceiling(bounceCount / Convert.ToDouble(pageSize));
			return View(new SendReportBounces(WebInterfaceLib.DAL.TransactionDB.GetDeferralInfo(sendID, page, pageSize), sendID, page, pageCount));
		}

		//
		// GET: /Sends/Failed?sendID=
		public ActionResult Failed(string sendID, int page = 1, int pageSize = 25)
		{
			int bounceCount = WebInterfaceLib.DAL.TransactionDB.GetFailedCount(sendID);
			int pageCount = (int)Math.Ceiling(bounceCount / Convert.ToDouble(pageSize));
			return View(new SendReportBounces(WebInterfaceLib.DAL.TransactionDB.GetFailedInfo(sendID, page, pageSize), sendID, page, pageCount));
		}

		//
		// GET: /Sends/Speed?sendID=
		public ActionResult Speed(string sendID)
		{
			return View(new SendReportSpeed(WebInterfaceLib.DAL.TransactionDB.GetSendSpeedInfo(sendID), sendID));
		}

		//
		// GET: /Sends/Pause?sendID=
		public ActionResult Pause(string sendID, string redirectURL)
		{
			SendManager.Instance.SetSendStatus(sendID, MantaMTA.Core.Enums.SendStatus.Paused);
			return View(new SendStatusUpdated(redirectURL));
		}

		//
		// GET: /Sends/Resume?sendID=
		public ActionResult Resume(string sendID, string redirectURL)
		{
			SendManager.Instance.SetSendStatus(sendID, MantaMTA.Core.Enums.SendStatus.Active);
			return View(new SendStatusUpdated(redirectURL));
		}

		//
		// GET: /Sends/Discard?sendID=
		public ActionResult Discard(string sendID, string redirectURL)
		{
			SendManager.Instance.SetSendStatus(sendID, MantaMTA.Core.Enums.SendStatus.Discard);
			return View(new SendStatusUpdated(redirectURL));
		}

		//
		// GET: /Sends/GetMessageResultCsv?sendID=
		public ActionResult GetMessageResultCsv(string sendID)
		{
			using (SqlConnection conn = MantaMTA.Core.DAL.MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
DECLARE @sendInternalID int
SELECT @sendInternalID = mta_send_internalId
FROM man_mta_send
WHERE mta_send_id = @sndID

SELECT *
FROM (
SELECT mta_msg_rcptTo AS 'RCPT', 
	(SELECT MAX(mta_transaction_timestamp) FROM man_mta_transaction as [tran] WHERE [tran].mta_msg_id = [msg].mta_msg_id) as 'Timestamp',
	(SELECT TOP 1 mta_transactionStatus_id FROM man_mta_transaction as [tran] WHERE [tran].mta_msg_id = [msg].mta_msg_id ORDER BY [tran].mta_transaction_timestamp DESC) as 'Status',
	(SELECT TOP 1 mta_transaction_serverHostname FROM man_mta_transaction as [tran] WHERE [tran].mta_msg_id = [msg].mta_msg_id ORDER BY [tran].mta_transaction_timestamp DESC) as 'Remote',
	(SELECT TOP 1 mta_transaction_serverResponse FROM man_mta_transaction as [tran] WHERE [tran].mta_msg_id = [msg].mta_msg_id ORDER BY [tran].mta_transaction_timestamp DESC) as 'Response'
FROM man_mta_msg as [msg]
WHERE [msg].mta_send_internalId = @sendInternalID ) as [ExportData]
ORDER BY [ExportData].Timestamp ASC";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				DataTable dt = new DataTable();
				conn.Open();
				SqlDataAdapter da = new SqlDataAdapter(cmd);
				da.Fill(dt);
				return View(dt);
			}
		}
    }
}
