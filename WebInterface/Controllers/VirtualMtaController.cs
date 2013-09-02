using System.Collections.Generic;
using System.Web.Mvc;
using MantaMTA.Core.DAL;
using MantaMTA.Core.MtaIpAddress;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class VirtualMtaController : Controller
    {
        //
        // GET: /VirtualMta/
        public ActionResult Index()
        {
			MtaIpAddressCollection ips = MtaIpAddressDB.GetMtaIpAddresses();
			List<VirtualMTASummary> summary = new List<VirtualMTASummary>();
			MtaIPGroupCollection ipGroups = WebInterfaceLib.MtaIpManager.GetAllIpGroups();
			foreach (MtaIpAddress address in ips)
			{
				summary.Add(new VirtualMTASummary 
				{ 
					IpAddress = address, 
					SendTransactionSummaryCollection = WebInterfaceLib.DAL.VirtualMtaTransactionDB.GetSendSummaryForIpAddress(address.ID)
				});
			}
			return View(new VirtualMtaPageModel { VirtualMTASummaryCollection = summary.ToArray(), IpGroups = ipGroups });
        }

    }
}
