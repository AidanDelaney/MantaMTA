using System;
using System.Collections.Concurrent;
using System.Linq;



namespace MantaMTA.Core.DNS
{
	internal static class DNSManager
	{
		/// <summary>
		/// Holds a thread safe collection of MX Records so we don't need to query the DNS API every time.
		/// </summary>
		private static ConcurrentDictionary<string, MXRecord[]> _Records = new ConcurrentDictionary<string, MXRecord[]>();

		/// <summary>
		/// Gets an Array of MX Records for the specified domain. If none found returns null.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		public static MXRecord[] GetMXRecords(string domain)
		{
			// This is what we'll be returning.
			MXRecord[] mxRecords = null;

			try
			{
				// Try and get cached records
				mxRecords = _Records[domain.ToLower()];
				
				// Look for cached records
				if (mxRecords != null)
				{
					// Make sure they haven't expired.
					if (mxRecords.Count(mx => mx.Dead) < 1)
						return mxRecords;
				}
			}
			catch (Exception) { /* Key doesn't exist in dictionary. */ }

			
			// Get the records from DNS
			string[] records = dnsapi.GetMXRecords(domain);
			
			// No MX records for domain.
			if (records == null)
				return null;
			else
				// Order by preferance
				records = records.OrderBy(s => s).ToArray();

			mxRecords = new MXRecord[records.Length];
			for (int i = 0; i < mxRecords.Length; i++)
			{
				string[] split = records[i].Split(new char[]{','});
				mxRecords[i] = new MXRecord(split[1], uint.Parse(split[0]), uint.Parse(split[2]));
			}

			_Records[domain.ToLower()] = mxRecords;
			return mxRecords;
		}
	}


}
