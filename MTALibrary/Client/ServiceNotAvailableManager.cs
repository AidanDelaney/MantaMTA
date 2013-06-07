using System;
using System.Collections.Concurrent;

namespace Colony101.MTA.Library.Client
{
	/// <summary>
	/// 421 Service not avalible likley means that an IP is being blocked.
	/// This manager helps us by keeping tack of these and we can use it to 
	/// only send a maximum of 1 message/minute from IPs that seem to be blocked.
	/// </summary>
	internal static class ServiceNotAvailableManager
	{	
		/// <summary>
		/// Dictionary<IPAddress, Dictionary<Hostname, LastUnavalible>
		/// </summary>
		public static ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>> _ServiceUnavalibleLog = new ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>>();

		/// <summary>
		/// Add a service unavalible event.
		/// </summary>
		/// <param name="ip"></param>
		/// <param name="mxHostname"></param>
		/// <param name="lastAvalible"></param>
		public static void Add(string ip, string mxHostname, DateTime lastFail)
		{
			mxHostname = mxHostname.ToLower();
			_ServiceUnavalibleLog.TryAdd(ip, new ConcurrentDictionary<string, DateTime>());
			ConcurrentDictionary<string, DateTime> ipServices = _ServiceUnavalibleLog[ip];
			ipServices.AddOrUpdate(mxHostname, lastFail, new Func<string,DateTime,DateTime>(delegate(string key, DateTime existingValue)
				{
					// We should only use the "new" timestamp if it's a later date that the existing value.
					// If the existing value is "newer" then a different thread updated already with better data.
					if (existingValue > lastFail)
						return existingValue;
					return lastFail;
				}));
		}

		/// <summary>
		/// Chek to see if the MX hostname has denied the specified IP access within the last 1 minute.
		/// </summary>
		/// <param name="ip">IP to check</param>
		/// <param name="mxHostname">Hostname of the MX to check</param>
		/// <returns>TRUE if service is unavalible</returns>
		public static bool IsServiceUnavalible(string ip, string mxHostname)
		{
			mxHostname = mxHostname.ToLower();
			ConcurrentDictionary<string, DateTime> ipServices = null;
			if (_ServiceUnavalibleLog.TryGetValue(ip, out ipServices))
			{
				DateTime lastFail = DateTime.MinValue;

				if (ipServices.TryGetValue(mxHostname, out lastFail))
				{
					if ((DateTime.Now - lastFail) < new TimeSpan(0, 1, 0))
						return true;
				}
			}

			return false;
		}
	}
}
