using System.Collections.Generic;
using System.Web.Mvc;
using MantaMTA.Core.DAL;
using MantaMTA.Core.VirtualMta;
using WebInterface.Models;
using WebInterfaceLib;

namespace WebInterface.Controllers
{
    public class VirtualMtaController : Controller
    {
        //
        // GET: /VirtualMta/
        public ActionResult Index()
        {
			VirtualMTACollection ips = VirtualMtaDB.GetVirtualMtas();
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
		public ActionResult Edit(int id = WebInterfaceParameters.VIRTUALMTA_NEW_ID)
		{
			if (id == WebInterfaceParameters.VIRTUALMTA_NEW_ID)
				return View(new VirtualMTA());
			return View(VirtualMtaDB.GetVirtualMta(id));
		}

		//
		// GET: /VirtualMta/EditGroup
		public ActionResult EditGroup(int id = WebInterfaceParameters.VIRTUALMTAGROUP_NEW_ID)
		{
			VirtualMtaGroup grp = null;
			if (id == WebInterfaceParameters.VIRTUALMTAGROUP_NEW_ID)
				grp = new VirtualMtaGroup();
			else
				grp = WebInterfaceLib.VirtualMtaWebManager.GetVirtualMtaGroup(id);

			return View(new VirtualMtaGroupCreateEditModel
			{
				VirtualMtaGroup = grp,
				VirtualMTACollection = VirtualMtaDB.GetVirtualMtas()
			});
		}
    }
}
