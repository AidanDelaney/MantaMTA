using System;
using System.Collections;
using System.Collections.Concurrent;

namespace MantaMTA.Core.OutboundRules
{
	internal static class ThrottleManager
	{
		/// <summary>
		/// <IpAddress, <MxPatternID, DateTime[]>
		/// </summary>
		private static ConcurrentDictionary<string, ConcurrentDictionary<int, ArrayList>> _sendHistory = new ConcurrentDictionary<string, ConcurrentDictionary<int, ArrayList>>();

		/// <summary>
		/// Gets permission to attempt a send of a message.
		/// </summary>
		/// <param name="ipAddress">The IP Address we wan't to send from.</param>
		/// <param name="mxRecord">The MX Record of the destination.</param>
		/// <returns>TRUE if we can send FALSE if we should throttle.</returns>
		public static bool TryGetSendAuth(MtaIpAddress.MtaIpAddress ipAddress, DNS.MXRecord mxRecord)
		{
			int mxPatternID = -1;
			int maxMessagesHour = OutboundRuleManager.GetMaxMessagesDestinationHour(ipAddress, mxRecord, out mxPatternID);

			// If the Max messages is -1 then unlimited so can just return true here.
			if (maxMessagesHour == -1)
				return true;

			int maxMessages = 0;
			int maxMessagesIntervalMinute = 0;
			while (maxMessages < 1 && maxMessagesIntervalMinute <= 60)
			{
				maxMessagesIntervalMinute++;
				maxMessages = (int)Math.Floor((maxMessagesHour / 60d) * maxMessagesIntervalMinute);
			}

			ConcurrentDictionary<int, ArrayList> mxSndHist = _sendHistory.GetOrAdd(ipAddress.IPAddress.ToString(), new ConcurrentDictionary<int,ArrayList>());
			ArrayList sndHistory = mxSndHist.GetOrAdd(mxPatternID, new ArrayList());

			ArrayList cleanedList = new ArrayList();
			for (int i = 0; i < sndHistory.Count; i++)
			{
				if (((DateTime)sndHistory[i]).AddMinutes(maxMessagesIntervalMinute) > DateTime.Now)
					cleanedList.Add(sndHistory[i]);
			}
			sndHistory = cleanedList;

			if (sndHistory.Count < maxMessages)
			{
				sndHistory.Add(DateTime.Now);
				mxSndHist[mxPatternID] = sndHistory;
				return true;
			}
			else
				return false;
		}
	}
}
