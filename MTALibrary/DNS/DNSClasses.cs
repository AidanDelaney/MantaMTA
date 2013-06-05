using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colony101.MTA.Library.DNS
{
	internal class MXRecord
	{
		/// <summary>
		/// The hostname or IP address of a MX
		/// </summary>
		public string Host { get; set; }

		/// <summary>
		/// The preference of the MX
		/// </summary>
		public uint Preference { get; set; }

		/// <summary>
		/// The TTL seconds
		/// </summary>
		private uint TTL { get; set; }

		/// <summary>
		/// The date/time that this record was got.
		/// </summary>
		private DateTime LookupTimestamp { get; set; }

		/// <summary>
		/// Return true if Time To Live has passed. RIP.
		/// </summary>
		public bool Dead
		{
			get
			{
				// If the TTL added to the lookup date is over specified date time then return true.
				return (LookupTimestamp.AddSeconds(this.TTL) < DateTime.Now);
			}
		}

		public MXRecord(string host, uint preference, uint ttl)
		{
			this.Host = host;
			this.Preference = preference;
			this.TTL = ttl;
			this.LookupTimestamp = DateTime.Now;
		}
	}


	[Serializable]
	internal class DNSDomainNotFoundException : Exception { }
}
