using MantaMTA.Core.DNS;
using MantaMTA.Core.VirtualMta;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MantaMTA.Core.OutboundRules
{
	internal class ThrottleManager
	{
		#region Singleton
		public static ThrottleManager Instance { get { return _Instance; } }
		private static readonly ThrottleManager _Instance = new ThrottleManager();
		private ThrottleManager() { }
		#endregion

		/// <summary>
		/// <IpAddress, <MxPatternID, DateTime[]>
		/// </summary>
		private IPSendHistory _sendHistory = new IPSendHistory();

		/// <summary>
		/// Holds an IP addresses send history grouped by MX Pattern ID
		/// </summary>
		private class IPSendHistory : ConcurrentDictionary<string, ThrottleManager.MxPatternThrottlingSendHistory> { }

		/// <summary>
		/// Class holds history of sends for an MxPattern
		/// </summary>
		private class MxPatternThrottlingSendHistory : ConcurrentDictionary<int, List<DateTime>>
		{
			/// <summary>
			/// Holds the maximum amount of messages that should be sent to this
			/// mx pattern in IntervalMinutes.
			/// </summary>
			public int IntervalMaxMessages { get; set; }

			/// <summary>
			/// Holds the minutes that IntervalMaxMessages can be sent in.
			/// </summary>
			public int IntervalMinutes { get; set; }

			/// <summary>
			/// Holds a timestamp of when the IntervalMinutes & IntervalMaxMessages 
			/// should next be recalculated.
			/// </summary>
			public DateTime IntervalValuesNeedRecalcTimestamp { get; set; }

			public MxPatternThrottlingSendHistory()
			{
				this.IntervalMinutes = -1;
				this.IntervalMaxMessages = -1;
				this.IntervalValuesNeedRecalcTimestamp = DateTime.UtcNow;
			}
		}

		/// <summary>
		/// Thread will be used to run in background removing old values from _sendHistory.
		/// </summary>
		private Thread _SendHistoryCleaner { get; set; }

		/// <summary>
		/// Start the _SendHistoryCleaner if it isn't running.
		/// </summary>
		private void StartSendHistoryCleaner()
		{
			if (this._SendHistoryCleaner == null ||
				this._SendHistoryCleaner.ThreadState == System.Threading.ThreadState.Stopped ||
				this._SendHistoryCleaner.ThreadState == System.Threading.ThreadState.Suspended ||
				this._SendHistoryCleaner.ThreadState == System.Threading.ThreadState.Aborted)
			{
				this._SendHistoryCleaner = new Thread(new ThreadStart(this.DoSendHistoryCleaning));
				this._SendHistoryCleaner.IsBackground = true;
				this._SendHistoryCleaner.Start();
			}
		}

		/// <summary>
		/// Keeps the send history clean by removing old values.
		/// This should only be called on background thread as it will run forever.
		/// </summary>
		private void DoSendHistoryCleaning()
		{
			while (true)
			{
				// Stopwatch is used to time the cleaning process.
				Stopwatch timer = new Stopwatch();
				timer.Start();
				// Loop through all outbound IPs send history
				foreach (KeyValuePair<string, ThrottleManager.MxPatternThrottlingSendHistory> ipHistory in this._sendHistory)
				{
					ThrottleManager.MxPatternThrottlingSendHistory ipMxPtnHistory = ipHistory.Value;
					// Loop through each MX Pattern within each outbound IP
					foreach (KeyValuePair<int, List<DateTime>> mxPatternHistory in ipMxPtnHistory)
					{
						// Lock the ArrayList that contains the send history.
						lock (mxPatternHistory.Value)
						{
							// ArrayList will hold the position of elements to remove from mxPatternHistory.Value
							ArrayList toRemove = new ArrayList();

							// Go through every log send and check that it hasn't expired.
							for (int i = 0; i < mxPatternHistory.Value.Count; i++)
							{
								if (mxPatternHistory.Value[i].AddMinutes((double)ipMxPtnHistory.IntervalMinutes) < DateTime.UtcNow)
									toRemove.Add(i);
							}

							// Remove send history that is no longer required.
							for (int z = toRemove.Count - 1; z >= 0; z--)
								mxPatternHistory.Value.RemoveAt((int)toRemove[z]);
						}
					}
				}

				// We don't wan't to have the cleaner thread running indefinitely if there isn't anything
				// to do. Sleep thread so it only runs once every 15 seconds. Unless it's taking longer than
				// 15 seconds to clean in which case go again instantly.
				timer.Stop();
				TimeSpan ts = (TimeSpan.FromSeconds(15) - timer.Elapsed);
				if (ts > TimeSpan.FromSeconds(0))
					Thread.Sleep(ts);
			}
		}

		/// <summary>
		/// Gets permission to attempt a send of a message.
		/// </summary>
		/// <param name="ipAddress">The IP Address we wan't to send from.</param>
		/// <param name="mxRecord">The MX Record of the destination.</param>
		/// <returns>TRUE if we can send FALSE if we should throttle.</returns>
		public bool TryGetSendAuth(VirtualMTA ipAddress, MXRecord mxRecord)
		{
			int mxPatternID = -1;
			int maxMessagesHour = OutboundRuleManager.GetMaxMessagesDestinationHour(ipAddress, mxRecord, out mxPatternID);

			// If the Max messages is -1 then unlimited so can just return true here.
			// No need for any logging or calculating.
			if (maxMessagesHour == -1)
				return true;

			// Create or get this outbound IP/mx pattern send history.
			ThrottleManager.MxPatternThrottlingSendHistory mxSndHist = null;
			if (!this._sendHistory.TryGetValue(ipAddress.IPAddress.ToString(), out mxSndHist))
			{
				mxSndHist = new ThrottleManager.MxPatternThrottlingSendHistory();
				this._sendHistory.AddOrUpdate(ipAddress.IPAddress.ToString(), mxSndHist, (string s, ThrottleManager.MxPatternThrottlingSendHistory sh) => mxSndHist);
			}
			//this._sendHistory.GetOrAdd(ipAddress.IPAddress.ToString(), new ThrottleManager.MxPatternThrottlingSendHistory());
			List<DateTime> sndHistory = mxSndHist.GetOrAdd(mxPatternID, new List<DateTime>());

			// Only calculate if needed.
			if (mxSndHist.IntervalValuesNeedRecalcTimestamp <= DateTime.UtcNow)
			{
				int maxMessages = 0;
				int maxMessagesIntervalMinute = 0;
				while (maxMessages < 1 && maxMessagesIntervalMinute <= 60)
				{
					maxMessagesIntervalMinute++;
					maxMessages = (int)Math.Floor((double)maxMessagesHour / 60d * (double)maxMessagesIntervalMinute);
				}

				mxSndHist.IntervalMaxMessages = maxMessages;
				mxSndHist.IntervalMinutes = maxMessagesIntervalMinute;
				mxSndHist.IntervalValuesNeedRecalcTimestamp = DateTime.UtcNow.AddMinutes(MtaParameters.MTA_CACHE_MINUTES);
			}


			lock (sndHistory)
			{
				// Remove sends that happened over "Interval" minute(s) ago.
				DateTime sendsAfterTimestamp = DateTime.UtcNow.AddMinutes((double)(mxSndHist.IntervalMinutes * -1));
				sndHistory.RemoveAll((DateTime d) => d <= sendsAfterTimestamp);

				// Check for throttling
				if (sndHistory.Count < mxSndHist.IntervalMaxMessages)
				{
					// Not hit throttle limit yet.
					// Log send and return true.
					sndHistory.Add(DateTime.UtcNow);
					return true;
				}
				else
					// THROTTLED!
					return false;
			}
		}
	}
}
