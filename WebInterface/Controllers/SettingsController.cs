using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MantaMTA.Core.DAL;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class SettingsController : Controller
    {
        //
        // GET: /Settings/
        public ActionResult Index()
        {
			return View(new SettingsModel
			{
				ClientIdleTimeout = CfgPara.GetClientIdleTimeout(),
				DaysToKeepSmtpLogsFor = CfgPara.GetDaysToKeepSmtpLogsFor(),
				DefaultVirtualMtaGroupID = CfgPara.GetDefaultVirtualMtaGroupID(),
				VirtualMtaGroupCollection = VirtualMtaGroupDB.GetVirtualMtaGroups(),			
				EventForwardingUrl = CfgPara.GetEventForwardingHttpPostUrl(),
				LocalDomains = CfgLocalDomains.GetLocalDomainsArray(),
				MaxTimeInQueue = CfgPara.GetMaxTimeInQueueMinutes(),
				ReceiveTimeout = CfgPara.GetReceiveTimeout(),
				RelayingPermittedIPs = CfgRelayingPermittedIP.GetRelayingPermittedIPAddresses(),
				RetryInterval = CfgPara.GetRetryIntervalBaseMinutes(),
				ReturnPathDomain = CfgPara.GetReturnPathDomain(),
				SendTimeout = CfgPara.GetSendTimeout()
			});
        }

    }
}
