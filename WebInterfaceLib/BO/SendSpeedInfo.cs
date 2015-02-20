using System;
using System.Collections.Generic;
using MantaMTA.Core.Enums;
using System.Linq;

namespace WebInterfaceLib.BO
{
	/// <summary>
	/// Holds speed information about a send.
	/// </summary>
	public class SendSpeedInfo : List<SendSpeedInfoItem>
	{
		public SendSpeedInfo() { }
		public SendSpeedInfo(IEnumerable<SendSpeedInfoItem> collection) : base(collection) { }

		/// <summary>
		/// Gets the first date for this SendSpeedInfoItem. Will be sending started for Reports. 1 hour ago for dashboard.
		/// </summary>
		public DateTime FirstTransactionTimestamp
		{
			get
			{
				return Dates.Min();
			}
		}

		/// <summary>
		/// Gets the last date for this SendSpeedInfoItem. Will be last transaction for Reports. now for dashboard.
		/// </summary>
		public DateTime LastTransactionTimestamp
		{
			get
			{
				return Dates.Max();
			}
		}

		/// <summary>
		/// An array of all of the timestamps that make up the send speed info.
		/// </summary>
		public DateTime[] Dates
		{

			get
			{
				if (_Dates == null)
				{
					List<DateTime> dates = new List<DateTime>();
					foreach (SendSpeedInfoItem item in this)
					{
						if (!dates.Contains(item.Timestamp))
							dates.Add(item.Timestamp);
					}
					_Dates = dates.ToArray();
				}

				return _Dates;
			}
		}
		private DateTime[] _Dates = null;

		/// <summary>
		/// Gets the speed data for a timestamp.
		/// </summary>
		/// <param name="timestamp">The timestamp to get data for.</param>
		/// <param name="accepted">returns the accepted rate.</param>
		/// <param name="failed">returns the failed rate.</param>
		/// <param name="deferred">returns the deferred rate.</param>
		public void GetDataPoints(DateTime timestamp, out long accepted, out long failed, out long deferred)
		{
			SendSpeedInfo subItems = new SendSpeedInfo(from i in this where i.Timestamp == timestamp select i);
			SendSpeedInfoItem item = subItems.SingleOrDefault(i => i.Status == TransactionStatus.Success);
			if (item != null)
				accepted = item.Count;
			else
				accepted = 0;

			item = subItems.SingleOrDefault(i => i.Status == TransactionStatus.Deferred);
			if (item != null)
				deferred = item.Count;
			else
				deferred = 0;


			failed = 0;
			foreach (SendSpeedInfoItem failedItem in subItems.Where(i => i.Status == TransactionStatus.Discarded || i.Status == TransactionStatus.Failed || i.Status == TransactionStatus.TimedOut))
				failed += failedItem.Count;
		}
	}

	/// <summary>
	/// Information about a sends speed.
	/// </summary>
	public class SendSpeedInfoItem 
	{
		/// <summary>
		/// The timestamp this item represents.
		/// </summary>
		public DateTime Timestamp { get; set; }

		/// <summary>
		/// The status this item relates to.
		/// </summary>
		public TransactionStatus Status { get; set; }

		/// <summary>
		/// The ammount of times this status happened in the timestamp for a send.
		/// </summary>
		public long Count { get; set; }
	}
}
