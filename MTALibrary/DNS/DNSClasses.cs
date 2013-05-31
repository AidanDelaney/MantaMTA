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
		/// The preferance of the MX
		/// </summary>
		public uint Preferance { get; set; }

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

		public MXRecord(string host, uint preferance, uint ttl)
		{
			this.Host = host;
			this.Preferance = preferance;
			this.TTL = ttl;
			this.LookupTimestamp = DateTime.Now;
		}
	}


	
	internal class DNSDomainNotFoundException : Exception { }
}
