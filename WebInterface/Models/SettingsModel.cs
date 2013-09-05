using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MantaMTA.Core.VirtualMta;

namespace WebInterface.Models
{
	/// <summary>
	/// Model for the Settings page.
	/// </summary>
	public class SettingsModel
	{
		public int ClientIdleTimeout { get; set; }
		public int DaysToKeepSmtpLogsFor { get; set; }
		public VirtualMtaGroup DefaultVirtualMtaGroup { get; set; }
		public string EventForwardingUrl { get; set; }
		public string[] LocalDomains { get; set; }
		public int MaxTimeInQueue { get; set; }
		public string[] RelayingPermittedIPs { get; set; }
		public int ReceiveTimeout { get; set; }
		public int RetryInterval { get; set; }
		public string ReturnPathDomain { get; set; }
		public int SendTimeout { get; set; }
	}
}