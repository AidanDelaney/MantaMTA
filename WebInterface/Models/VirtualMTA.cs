using System;
using System.Linq;
using System.Net;
using MantaMTA.Core.Enums;
using MantaMTA.Core.VirtualMta;
using WebInterfaceLib.BO;

namespace WebInterface.Models
{
	/// <summary>
	/// Holds information about a virtual MTA & it's sends.
	/// Used in Virtual MTA page models.
	/// </summary>
	public class VirtualMTASummary
	{
		/// <summary>
		/// The IP Address that this virtual MTA represents.
		/// </summary>
		public VirtualMTA IpAddress { get; set; }
		/// <summary>
		/// Iformation about sends from this virtual MTA.
		/// </summary>
		public SendTransactionSummaryCollection SendTransactionSummaryCollection { get; set; }

		/// <summary>
		/// Returns TRUE if the Virtual MTA Hostname matches a rDNS lookup.
		/// </summary>
		public bool IsReverseDnsMatch
		{
			get
			{
				try
				{
					return Dns.GetHostEntry(IpAddress.IPAddress).HostName.Equals(IpAddress.Hostname, StringComparison.OrdinalIgnoreCase);
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// The ammount of messages sent from this vMTA that were accepted by the remote MX.
		/// </summary>
		public long Accepted
		{
			get
			{
				SendTransactionSummary info = SendTransactionSummaryCollection.SingleOrDefault(i => i.Status == TransactionStatus.Success);
				if (info == null)
					return 0;
				return info.Count;
			}
		}

		/// <summary>
		/// The ammount of messages sent from this vMTA that were not accepted by the remote MX.
		/// </summary>
		public long Failed
		{
			get
			{
				SendTransactionSummary info = SendTransactionSummaryCollection.SingleOrDefault(i => i.Status == TransactionStatus.Discarded ||
																								    i.Status == TransactionStatus.Failed ||
																									i.Status == TransactionStatus.TimedOut);
				if (info == null)
					return 0;
				return info.Count;
			}
		}

		/// <summary>
		/// The percentage of message send attempts that where going to be sent from this vMTA that 
		/// didn't due to MantaMTA throttling.
		/// </summary>
		public double ThrottledPercent
		{
			get
			{
				long attempts = SendTransactionSummaryCollection.Sum(i => i.Count);
				if (attempts == 0)
					return 0;

				SendTransactionSummary info = SendTransactionSummaryCollection.SingleOrDefault(i => i.Status == TransactionStatus.Throttled);
				if (info == null || info.Count == 0)
					return 0;
				return (100d / attempts) * info.Count;
			}
		}

		/// <summary>
		/// The percentage of message send attempts that where temp rejected by the remote MX.
		/// </summary>
		public double DeferredPercent
		{
			get
			{
				long attempts = SendTransactionSummaryCollection.Sum(i => i.Count);
				if (attempts == 0)
					return 0;

				SendTransactionSummary info = SendTransactionSummaryCollection.SingleOrDefault(i => i.Status == TransactionStatus.Deferred);
				if (info == null || info.Count == 0)
					return 0;
				return (100d / attempts) * info.Count;
			}
		}
	}

	/// <summary>
	/// Data model for the Virtual MTA page.
	/// </summary>
	public class VirtualMtaPageModel
	{
		/// <summary>
		/// Collection of all the Virtual MTA's
		/// </summary>
		public VirtualMTASummary[] VirtualMTASummaryCollection { get; set; }
		/// <summary>
		/// Collection of the vMTA Groups
		/// </summary>
		public VirtualMtaGroupCollection IpGroups { get; set; }
	}

	/// <summary>
	/// Model for the Create & Edit Virtual MTA Group Page.
	/// </summary>
	public class VirtualMtaGroupCreateEditModel
	{
		/// <summary>
		/// The Virtual MTA Group.
		/// </summary>
		public VirtualMtaGroup VirtualMtaGroup { get; set; }
		/// <summary>
		/// All Virtual MTAs.
		/// </summary>
		public VirtualMTACollection VirtualMTACollection { get; set; }
	}
}