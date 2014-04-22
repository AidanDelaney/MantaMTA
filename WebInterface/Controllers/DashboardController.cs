using System.Web.Mvc;
using WebInterface.Models;
using MantaMTA.Core.Enums;

namespace WebInterface.Controllers
{
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/
        public ActionResult Index()
        {
			return View(new DashboardModel
				{
					SendTransactionSummaryCollection = WebInterfaceLib.DAL.TransactionDB.GetLastHourTransactionSummary(),
					Waiting = WebInterfaceLib.DAL.SendDB.GetQueueCount(new SendStatus[]{ SendStatus.Active, SendStatus.Discard }),
					Paused = WebInterfaceLib.DAL.SendDB.GetQueueCount(new SendStatus[] { SendStatus.Paused }),
					BounceInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourBounceInfo(3),
					SendSpeedInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourSendSpeedInfo()
				});
        }

    }
}
