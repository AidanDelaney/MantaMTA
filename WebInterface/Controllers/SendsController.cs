using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MantaMTA.Core.ServiceContracts;
using WebInterface.Models;
using WebInterfaceLib.BO;

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
			ISendManagerContract sendManager = ServiceContractManager.GetServiceChannel<ISendManagerContract>();
			int internalID = MantaMTA.Core.Sends.SendManager.Instance.GetSend(sendID).InternalID;
			sendManager.Pause(internalID);
			return View(new SendStatusUpdated(redirectURL));
		}

		//
		// GET: /Sends/Resume?sendID=
		public ActionResult Resume(string sendID, string redirectURL)
		{
			ISendManagerContract sendManager = ServiceContractManager.GetServiceChannel<ISendManagerContract>();
			int internalID = MantaMTA.Core.Sends.SendManager.Instance.GetSend(sendID).InternalID;
			sendManager.Resume(internalID);
			return View(new SendStatusUpdated(redirectURL));
		}

		//
		// GET: /Sends/Discard?sendID=
		public ActionResult Discard(string sendID, string redirectURL)
		{
			ISendManagerContract sendManager = ServiceContractManager.GetServiceChannel<ISendManagerContract>();
			int internalID = MantaMTA.Core.Sends.SendManager.Instance.GetSend(sendID).InternalID;
			sendManager.Discard(internalID);
			return View(new SendStatusUpdated(redirectURL));
		}
    }
}
