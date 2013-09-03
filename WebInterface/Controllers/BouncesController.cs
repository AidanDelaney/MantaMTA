using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class BouncesController : Controller
    {
        //
        // GET: /Bounces/
        public ActionResult Index(int? page = 1, int? pageSize = 25)
        {
			long deferred, rejected = 0;
			WebInterfaceLib.DAL.TransactionDB.GetBounceDeferredAndRejected(out deferred, out rejected);
			return View(new BounceModel
			{
				BounceInfo = WebInterfaceLib.DAL.TransactionDB.GetBounceInfo(null, page.Value, pageSize.Value),
				CurrentPage = page.Value,
				PageCount = (int)Math.Ceiling(Convert.ToDouble(WebInterfaceLib.DAL.TransactionDB.GetBounceCount(null)) / pageSize.Value),
				DeferredCount = deferred,
				RejectedCount = rejected
			});
        }

    }
}
