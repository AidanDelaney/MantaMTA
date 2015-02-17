using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.DNS
{
	public class MXRecord
	{
		/// <summary>
		/// The hostname or IP address of a MX
		/// </summary>
		public string Host { get; set; }

		/// <summary>
		/// The preference of the MX
		/// </summary>
		public int Preference { get; set; }

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
				return (LookupTimestamp.AddSeconds(this.TTL) < DateTime.UtcNow);
			}
		}

		public MXRecord(string host, int preference, uint ttl)
		{
			this.Host = host;
			this.Preference = preference;
			this.TTL = ttl;
			this.LookupTimestamp = DateTime.UtcNow;
		}
	}


	[Serializable]
	internal class DNSDomainNotFoundException : Exception { }
}
