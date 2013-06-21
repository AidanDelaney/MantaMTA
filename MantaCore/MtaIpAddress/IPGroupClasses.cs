using System;
using System.Linq;

namespace MantaMTA.Core.MtaIpAddress
{
	/// <summary>
	/// Represents a grouping of IP Address that can be used by the MTA for 
	/// sending of messages.
	/// </summary>
	public class MtaIPGroup
	{
		/// <summary>
		/// The groups unique identifier.
		/// </summary>
		public int ID { get; set; }
		/// <summary>
		/// Name of the Group
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Optional: Group description.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Collection of the IP Addresses that make up this group.
		/// </summary>
		public MtaIpAddressCollection IpAddresses { get; set; }

		/// <summary>
		/// Gets a random IP from the collection.
		/// This should be improved to take into account messages sent in last ?.
		/// </summary>
		/// <returns></returns>
		public MtaIpAddress GetRandomIP()
		{
			// There are no IP addresses in the group so return null.
			if (IpAddresses == null)
				return null;

			return IpAddresses.OrderBy(x => new Random().Next()).FirstOrDefault();
		}
	}
}
