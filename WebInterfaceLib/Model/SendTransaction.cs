using System.Collections.Generic;
using MantaMTA.Core.Enums;
using MantaMTA.Core.MtaIpAddress;

namespace WebInterfaceLib.Model
{
	/// <summary>
	/// Holds a Send Status transaction, this may be for a send or a vMTA.
	/// </summary>
	public class SendTransactionSummary
	{
		/// <summary>
		/// The TransactionStatus type that this object represents.
		/// </summary>
		public TransactionStatus Status { get; set; }

		/// <summary>
		/// The amount of time it has happened.
		/// </summary>
		public int Count { get; set; }

		public SendTransactionSummary(TransactionStatus status, int count)
		{
			Status = status;
			Count = count;
		}
	}

	/// <summary>
	/// Collection of Send Transaction Summaries.
	/// </summary>
	public class SendTransactionSummaryCollection : List<SendTransactionSummary>
	{
		public SendTransactionSummaryCollection() { }
		public SendTransactionSummaryCollection(IEnumerable<SendTransactionSummary> collection) : base(collection) { }
	}
}
