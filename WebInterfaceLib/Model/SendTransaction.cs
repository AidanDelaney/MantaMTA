using System.Collections.Generic;
using MantaMTA.Core.Enums;
using MantaMTA.Core.MtaIpAddress;

namespace WebInterfaceLib.Model
{
	public class SendTransactionSummary
	{
		public TransactionStatus Status { get; set; }
		public int Count { get; set; }

		public SendTransactionSummary(TransactionStatus status, int count)
		{
			Status = status;
			Count = count;
		}
	}

	public class SendTransactionSummaryCollection : List<SendTransactionSummary>
	{
		public SendTransactionSummaryCollection() { }
		public SendTransactionSummaryCollection(IEnumerable<SendTransactionSummary> collection) : base(collection) { }
	}
}
