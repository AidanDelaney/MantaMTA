using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Script.Services;
using System.Web.Services;
using MantaMTA.Core;
using MantaMTA.Core.DAL;

namespace WebInterface.Services
{
	/// <summary>
	/// Summary description for SettingsService
	/// </summary>
	[WebService(Namespace = "http://manta.io/mantamta/web")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[ScriptService]
	public class SettingsService : System.Web.Services.WebService
	{
		/// <summary>
		/// Saves the settings.
		/// </summary>
		/// <param name="clientIdleTimeout">Seconds before connection idle timeout.</param>
		/// <param name="receiveTimeout">Seconds before connection receive timeout.</param>
		/// <param name="sendTimeout">Seconds before connection send timeout.</param>
		/// <param name="defaultVirtualMtaGroupID">ID of the VirtualMTA Group to use if not specified in email headers.</param>
		/// <param name="eventUrl">URL to post events to.</param>
		/// <param name="daysToKeepSmtpLogsFor">Days to keep smtp logs defore deleing them.</param>
		/// <param name="maxTimeInQueueHours">Max time in hours that an email can be in the queue for before it is timed out.</param>
		/// <param name="retryIntervalBase">The time in minutes to use as the base for the retry interval calculation.</param>
		/// <param name="ipAddressesForRelaying">Array of the IP Addresses that are allowed to relay through MantaMTA.</param>
		/// <param name="returnPathLocalDomainID">ID of the local domain that should be used as the hostname for the returnpath if none is specified in the email headers.</param>
		/// <param name="localDomains">Array of the localdomains.</param>
		/// <returns></returns>
		[WebMethod]
		public bool Update(int clientIdleTimeout, int receiveTimeout, int sendTimeout, int defaultVirtualMtaGroupID, string eventUrl, int daysToKeepSmtpLogsFor, 
							int maxTimeInQueueHours, int retryIntervalBase, string[] ipAddressesForRelaying, int returnPathLocalDomainID, string[] localDomains)
		{
			if (clientIdleTimeout < 0 ||
				receiveTimeout < 0 ||
				sendTimeout < 0)
				return false;

			List<IPAddress> relayingIps = new List<IPAddress>();
			foreach (string str in ipAddressesForRelaying)
			{
				relayingIps.Add(IPAddress.Parse(str));
			}

			CfgPara.SetClientIdleTimeout(clientIdleTimeout);
			CfgPara.SetReceiveTimeout(receiveTimeout);
			CfgPara.SetSendTimeout(sendTimeout);
			CfgPara.SetDefaultVirtualMtaGroupID(defaultVirtualMtaGroupID);
			CfgPara.SetEventForwardingHttpPostUrl(eventUrl);
			CfgPara.SetDaysToKeepSmtpLogsFor(daysToKeepSmtpLogsFor);
			CfgPara.SetMaxTimeInQueueMinutes(maxTimeInQueueHours * 60);
			CfgPara.SetRetryIntervalBaseMinutes(retryIntervalBase);
			CfgRelayingPermittedIP.SetRelayingPermittedIPAddresses(relayingIps.ToArray());
			CfgPara.SetReturnPathLocalDomain(returnPathLocalDomainID);

			LocalDomainCollection domains = CfgLocalDomains.GetLocalDomainsArray();
			CfgLocalDomains.ClearLocalDomains();
			foreach (string localDomain in localDomains)
			{
				if (string.IsNullOrWhiteSpace(localDomain))
					continue;
				LocalDomain ld = domains.SingleOrDefault(d => d.Hostname.Equals(localDomain, StringComparison.OrdinalIgnoreCase));
				if (ld == null)
					ld = new LocalDomain { Hostname = localDomain.Trim() };
				CfgLocalDomains.Save(ld);
			}
			return true;
		}
	}
}
