using System.Collections.Generic;
using MantaMTA.Core.Sends;
using System.Linq;
using System;
using System.Text;

namespace WebInterfaceLib.Model
{
	public class SendInfoCollection : List<SendInfo>
	{
		public SendInfoCollection() { }
		public SendInfoCollection(IEnumerable<SendInfo> collection) : base(collection) { }

		/// <summary>
		/// 
		/// </summary>
		public int Waiting
		{
			get
			{
				return this.Sum(snd => snd.Waiting);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public double ThrottledPercent
		{
			get
			{
				int attempts = this.Sum(snd => snd.Attempts);
				if (attempts == 0)
					return 0;
				int throttled = this.Sum(snd => snd.Throttled);
				return (100d / attempts) * throttled;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public double DeferredPercent
		{
			get
			{
				int attempts = this.Sum(snd => snd.Attempts);
				if (attempts == 0)
					return 0;
				int deferred = this.Sum(snd => snd.Deferred);
				return (100d / attempts) * deferred;
			}
		}
	}

	public class SendInfo : Send
	{
		public int TotalMessages { get; set; }
		public int Accepted { get; set; }
		public int Rejected { get; set; }
		public int Waiting { get; set; }
		public int Throttled { get; set; }
		public int Deferred { get; set; }
		public DateTime? LastTransactionTimestamp { get; set; }

		public int Attempts
		{
			get
			{
				return Accepted + Rejected + Waiting + Throttled + Deferred;
			}
		}

		public double DeferredPercent
		{
			get
			{
				if (Attempts == 0)
					return 0;
				return (100d / Attempts) * Deferred;
			}
		}

		public double ThrottledPercent
		{
			get
			{
				if (Attempts == 0)
					return 0;
				return (100d / Attempts) * Throttled;
			}
		}

		public string SendTimeString
		{
			get
			{
				DateTime sendLastActive = DateTime.UtcNow;
				if (LastTransactionTimestamp != null && Waiting < 1)
					sendLastActive = LastTransactionTimestamp.Value;

				TimeSpan timeActive = sendLastActive - CreatedTimestamp;

				StringBuilder sb = new StringBuilder(string.Empty);
				if (timeActive.TotalDays >= 1)
					sb.AppendFormat("{0}d {1}h {2}m", timeActive.Days, timeActive.Hours, timeActive.Minutes);
				else if(timeActive.TotalHours >= 1)
					sb.AppendFormat("{0}h {0}m", timeActive.Hours, timeActive.Minutes);
				else
					sb.AppendFormat("{0}m", timeActive.Minutes);

				return sb.ToString();
			}
		}

		public SendInfo()
		{
			this.Accepted = 0;
			this.Deferred = 0;
			this.Rejected = 0;
			this.Throttled = 0;
			this.TotalMessages = 0;
			this.Waiting = 0;
			this.LastTransactionTimestamp = null;
		}
	}
}
