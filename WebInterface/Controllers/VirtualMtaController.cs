using System.Collections.Generic;
using System.Web.Mvc;
using MantaMTA.Core.DAL;
using MantaMTA.Core.VirtualMta;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class VirtualMtaController : Controller
    {
        //
        // GET: /VirtualMta/
        public ActionResult Index()
        {
			VirtualMTACollection ips = MtaIpAddressDB.GetMtaIpAddresses();
			List<VirtualMTASummary> summary = new List<VirtualMTASummary>();
			VirtualMtaGroupCollection ipGroups = WebInterfaceLib.VirtualMtaWebManager.GetAllVirtualMtaGroups();
			foreach (VirtualMTA address in ips)
			{
				summary.Add(new VirtualMTASummary 
				{ 
					IpAddress = address, 
					SendTransactionSummaryCollection = WebInterfaceLib.DAL.VirtualMtaTransactionDB.GetSendSummaryForIpAddress(address.ID)
				});
			}
			return View(new VirtualMtaPageModel { VirtualMTASummaryCollection = summary.ToArray(), IpGroups = ipGroups });
        }

		//
		// GET: /VirtualMta/Edit
		public ActionResult Edit(int id = 0)
		{
			if (id == 0)
				return View(new VirtualMTA());
			return View(MtaIpAddressDB.GetMtaIpAddress(id));
		}

		//
		// GET: /VirtualMta/EditGroup
		public ActionResult EditGroup(int id = 0)
		{
			return View(WebInterfaceLib.VirtualMtaWebManager.GetAllVirtualMtaGroups());
		}
    }
}
