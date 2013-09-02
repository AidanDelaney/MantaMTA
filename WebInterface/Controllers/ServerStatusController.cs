using System.Web.Mvc;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class ServerStatusController : Controller
    {
        //
        // GET: /ServerStatus/
        public ActionResult Index()
        {
            return View(new ServerStatusModel());
        }

    }
}
