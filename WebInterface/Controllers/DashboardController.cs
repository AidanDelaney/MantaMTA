using System.Web.Mvc;
using WebInterface.Models;

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
					Waiting = WebInterfaceLib.DAL.SendDB.GetWaitingCount(),
					BounceInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourBounceInfo(3),
					SendSpeedInfo = WebInterfaceLib.DAL.TransactionDB.GetLastHourSendSpeedInfo()
				});
        }

    }
}
