using MantaMTA.Core;
using MantaMTA.Core.VirtualMta;

namespace WebInterface.Models
{
	/// <summary>
	/// Model for the Settings page.
	/// </summary>
	public class SettingsModel
	{
		/// <summary>
		/// Time in seconds before connection idle timeout.
		/// </summary>
		public int ClientIdleTimeout { get; set; }

		/// <summary>
		/// The amount of days SMTP logs should be kept for before deleting.
		/// </summary>
		public int DaysToKeepSmtpLogsFor { get; set; }

		/// <summary>
		/// The ID of the Virtual MTA Group to use if none is specified in an emails header.
		/// </summary>
		public int DefaultVirtualMtaGroupID { get; set; }

		/// <summary>
		/// Collection of all the Virtual MTA Groups that Manta knows about.
		/// </summary>
		public VirtualMtaGroupCollection VirtualMtaGroupCollection { get; set; }

		/// <summary>
		/// The URL to forward events to.
		/// </summary>
		public string EventForwardingUrl { get; set; }

		/// <summary>
		/// Collection of the LocalDomains that Manta uses.
		/// </summary>
		public LocalDomainCollection LocalDomains { get; set; }

		/// <summary>
		/// Maximum time in minutes that any message should be allowed to spend in the queue before timeing out.
		/// </summary>
		public int MaxTimeInQueue { get; set; }

		/// <summary>
		/// Array of the IP Addresses that are allowed to relay through Manta.
		/// </summary>
		public string[] RelayingPermittedIPs { get; set; }

		/// <summary>
		/// Time in seconds before SMTP client should timeout when waiting for a response.
		/// </summary>
		public int ReceiveTimeout { get; set; }

		/// <summary>
		/// The base minutes to use for the retry interval calculation.
		/// </summary>
		public int RetryInterval { get; set; }

		/// <summary>
		/// The default hostname that should be used for returnpath domains if not set in email header.
		/// </summary>
		public string ReturnPathDomain { get; set; }

		/// <summary>
		/// The time in seconds to wait before timing out and SMTP client transmittion.
		/// </summary>
		public int SendTimeout { get; set; }
	}
}