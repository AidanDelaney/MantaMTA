using System;
using System.Collections.Generic;
using MantaMTA.Core.Enums;
using System.Linq;

namespace WebInterfaceLib.Model
{
	public class SendSpeedInfo : List<SendSpeedInfoItem>
	{
		public SendSpeedInfo() { }
		public SendSpeedInfo(IEnumerable<SendSpeedInfoItem> collection) : base(collection) { }

		private DateTime[] _Dates = null;
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

		public void GetDataPoints(DateTime timestamp, out int accepted, out int failed, out int deferred)
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
			foreach(SendSpeedInfoItem failedItem in subItems.Where(i=>i.Status == TransactionStatus.Discarded || i.Status == TransactionStatus.Failed || i.Status == TransactionStatus.TimedOut))
				failed += failedItem.Count;
		}
	}


	public class SendSpeedInfoItem
	{
		public TransactionStatus Status { get; set; }
		public DateTime Timestamp { get; set; }
		public int Count { get; set; }
	}
}
