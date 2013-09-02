using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MantaMTA.Core.MtaIpAddress;
using WebInterfaceLib.Model;
using MantaMTA.Core.Enums;

namespace WebInterface.Models
{
	public class VirtualMTASummary
	{
		public MtaIpAddress IpAddress { get; set; }
		public SendTransactionSummaryCollection SendTransactionSummaryCollection { get; set; }

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

	public class VirtualMtaPageModel
	{
		public VirtualMTASummary[] VirtualMTASummaryCollection { get; set; }
		public MtaIPGroupCollection IpGroups { get; set; }
	}
}