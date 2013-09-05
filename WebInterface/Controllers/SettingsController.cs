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
				DefaultVirtualMtaGroup = VirtualMtaGroupDB.GetVirtualMtaGroup(CfgPara.GetDefaultIPGroupID()),			
				EventForwardingUrl = CfgPara.GetEventForwardingHttpPostUrl(),
				LocalDomains = CfgLocalDomains.GetLocalDomainsArray(),
				MaxTimeInQueue = CfgPara.GetMaxTimeInQueueMinutes(),
				ReceiveTimeout = CfgPara.GetReceiveTimeout(),
				RelayingPermittedIPs = CfgRelayingPermittedIP.GetRelayingPermittedIPAddresses(),
				RetryInterval = CfgPara.GetRetryIntervalMinutes(),
				ReturnPathDomain = CfgPara.GetReturnPathDomain(),
				SendTimeout = CfgPara.GetSendTimeout()
			});
        }

    }
}
