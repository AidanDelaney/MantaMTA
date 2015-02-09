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
			// Make sure the domain is all lower.
			domain = domain.ToLower();

			// This is what we'll be returning.
			MXRecord[] mxRecords = null;

			// Try and get DNS from internal cache.
			if (DNSManager._Records.TryGetValue(domain, out mxRecords))
			{
				// Found cached records.
				// Make sure they haven't expired.
				if (mxRecords.Count((MXRecord mx) => mx.Dead) < 1)
					return mxRecords;
			}

			string[] records = null;

			try
			{
				// Get the records from DNS
				records = dnsapi.GetMXRecords(domain);
			}
			catch (DNSDomainNotFoundException)
			{
				// Ensure records is null.
				records = null;
			}

			// No MX records for domain.
			if (records == null)
			{
				// If there are no MX records use the hostname as per SMTP RFC.
				MXRecord[] mxs = new MXRecord[]
				{
					new MXRecord(domain, 10, 300u)
				};
				_Records.AddOrUpdate(domain, mxs, (string key, MXRecord[] existing) => mxs);
				return mxs;
			}

			mxRecords = new MXRecord[records.Length];
			for (int i = 0; i < mxRecords.Length; i++)
			{
				string[] split = records[i].Split(new char[] { ',' });
				if(split.Length == 3)
					mxRecords[i] = new MXRecord(split[1], int.Parse(split[0]), uint.Parse(split[2]));
			}

			// Order by preferance
			mxRecords = (
				from mx in mxRecords
				where mx != null
				orderby mx.Preference
				select mx).ToArray<MXRecord>();
			DNSManager._Records.TryAdd(domain, mxRecords);
			return mxRecords;
		}
	}
}
