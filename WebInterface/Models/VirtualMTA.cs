using System.Linq;
using MantaMTA.Core.Enums;
using MantaMTA.Core.MtaIpAddress;
using WebInterfaceLib.Model;

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
		public MtaIpAddress IpAddress { get; set; }
		/// <summary>
		/// Iformation about sends from this virtual MTA.
		/// </summary>
		public SendTransactionSummaryCollection SendTransactionSummaryCollection { get; set; }

		/// <summary>
		/// The ammount of messages sent from this vMTA that were accepted by the remote MX.
		/// </summary>
		public int Accepted
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
		public int Failed
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
				int attempts = SendTransactionSummaryCollection.Sum(i => i.Count);
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
				int attempts = SendTransactionSummaryCollection.Sum(i => i.Count);
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
		public MtaIPGroupCollection IpGroups { get; set; }
	}
}