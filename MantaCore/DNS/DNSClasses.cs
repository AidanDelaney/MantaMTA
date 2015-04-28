using System;

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

		/// <summary>
		/// Identifies the source of an MX record.
		/// </summary>
		public MxRecordSrc MxRecordSrc { get; set; }

		public MXRecord(string host, int preference, uint ttl, MxRecordSrc mxRecordSrc)
		{
			this.Host = host;
			this.Preference = preference;
			this.TTL = ttl;
			this.LookupTimestamp = DateTime.UtcNow;
			this.MxRecordSrc = mxRecordSrc;
		}
	}

	/// <summary>
	/// Identifies where the MX record came from.
	/// </summary>
	public enum MxRecordSrc
	{
		Unknown = 0,
		/// <summary>
		/// MX record exists in DNS.
		/// </summary>
		MX = 1,
		/// <summary>
		/// No MX record in DNS, using A instead.
		/// </summary>
		A = 2
	}


	[Serializable]
	internal class DNSDomainNotFoundException : Exception { }
}
