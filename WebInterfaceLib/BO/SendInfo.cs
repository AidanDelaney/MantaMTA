using System.Collections.Generic;
using MantaMTA.Core.Sends;
using System.Linq;
using System;
using System.Text;

namespace WebInterfaceLib.BO
{
	public class SendInfoCollection : List<SendInfo>
	{
		public SendInfoCollection() { }
		public SendInfoCollection(IEnumerable<SendInfo> collection) : base(collection) { }

		/// <summary>
		/// 
		/// </summary>
		public long Waiting
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
				long attempts = this.Sum(snd => snd.Attempts);
				if (attempts == 0)
					return 0;
				long throttled = this.Sum(snd => snd.Throttled);
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
				long attempts = this.Sum(snd => snd.Attempts);
				if (attempts == 0)
					return 0;
				long deferred = this.Sum(snd => snd.Deferred);
				return (100d / attempts) * deferred;
			}
		}
	}

	/// <summary>
	/// Information about a send.
	/// </summary>
	public class SendInfo : Send
	{
		/// <summary>
		/// The total messages that this send has.
		/// </summary>
		public long TotalMessages { get; set; }

		/// <summary>
		/// The amount of messages that have been accepted by remote MXs.
		/// </summary>
		public long Accepted { get; set; }

		/// <summary>
		/// Amount of messages that have been rejected by remote MXs.
		/// </summary>
		public long Rejected { get; set; }

		/// <summary>
		/// Amount of messages waiting to be sent.
		/// </summary>
		public long Waiting { get; set; }

		/// <summary>
		/// Amount of times that an attempt to send has been throttled.
		/// </summary>
		public long Throttled { get; set; }

		/// <summary>
		/// Amount of times that attempts to send messages have been deferred by remote MXs.
		/// </summary>
		public long Deferred { get; set; }

		/// <summary>
		/// Timestamp of the last transaction. NULL if no transactions.
		/// </summary>
		public DateTime? LastTransactionTimestamp { get; set; }

		/// <summary>
		/// The amount of attempts to send messages that have been made.
		/// </summary>
		public long Attempts
		{
			get
			{
				return Accepted + Rejected + Waiting + Throttled + Deferred;
			}
		}

		/// <summary>
		/// Percentage of attempts to send that where deferred.
		/// </summary>
		public double DeferredPercent
		{
			get
			{
				if (Attempts == 0)
					return 0;
				return (100d / Attempts) * Deferred;
			}
		}


		/// <summary>
		/// Percentage of attempts to send that where throttled.
		/// </summary>
		public double ThrottledPercent
		{
			get
			{
				if (Attempts == 0)
					return 0;
				return (100d / Attempts) * Throttled;
			}
		}

		/// <summary>
		/// The "Active Time" for the send.
		/// Active time is the Time between the send being created and the last transaction time.
		/// </summary>
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


		/// <summary>
		/// Constructor sets defaults.
		/// </summary>
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
