using System;
using System.Collections.Generic;
using System.Linq;

namespace MantaMTA.Core.VirtualMta
{
	/// <summary>
	/// Represents a grouping of IP Address that can be used by the MTA for 
	/// sending of messages.
	/// </summary>
	public class VirtualMtaGroup
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
		/// Collection of the Virtual MTAs that make up this group.
		/// </summary>
		public VirtualMTACollection VirtualMtaCollection { get; set; }
		/// <summary>
		/// Timestamp of when this MtaIPGroup instance was created; used for caching.
		/// </summary>
		public DateTime CreatedTimestamp = DateTime.UtcNow;

		/// <summary>
		/// Gets a random IP from the collection.
		/// This should be improved to take into account messages sent in last ?.
		/// </summary>
		/// <returns></returns>
		public VirtualMTA GetRandomIP()
		{
			// There are no IP addresses in the group so return null.
			if (VirtualMtaCollection == null)
				return null;

			return VirtualMtaCollection.OrderBy(x => new Random().Next()).FirstOrDefault();
		}

		/// <summary>
		/// Object used for locking in GetIpAddressForSending method.
		/// </summary>
		private object _SyncLock = new object();

		/// <summary>
		/// Gets an IP Address. Uses <paramref name="mxRecord"/> to load balance accross all IPs in group.
		/// </summary>
		/// <param name="mxRecord">MXRecord of the host wanting to send to.</param>
		/// <returns>MtaIpAddress or NULL if none in group.</returns>
		internal VirtualMTA GetVirtualMtasForSending(DNS.MXRecord mxRecord)
		{
			lock (_SyncLock)
			{
				string key = mxRecord.Host.ToLowerInvariant();

				// Get the IP address that has sent the least to the mx host.
				VirtualMTA vMTA = VirtualMtaCollection.OrderBy(ipAddr => ipAddr.SendsCounter.GetOrAdd(key, 0)).FirstOrDefault();
				
				// Get the current sends count.
				int currentSends = 0;
				if (!vMTA.SendsCounter.TryGetValue(key, out currentSends))
					return null;

				// Increment the sends count to include this one.
				vMTA.SendsCounter.AddOrUpdate(key, currentSends + 1, new Func<string, int, int>(delegate(string k, int value) { return value + 1; }));

				// Return the IP Address.
				return vMTA;
			}
		}

		/// <summary>
		/// Constuctor sets defaults.
		/// </summary>
		public VirtualMtaGroup()
		{
			this.Description = string.Empty;
			this.ID = 0;
			this.Name = string.Empty;
			this.VirtualMtaCollection = new VirtualMTACollection();
		}
	}

	/// <summary>
	/// Holds a collection of MtaIPGroup objects.
	/// </summary>
	public class VirtualMtaGroupCollection : List<VirtualMtaGroup>
	{
		public VirtualMtaGroupCollection() { }
		public VirtualMtaGroupCollection(IEnumerable<VirtualMtaGroup> collection) : base(collection) { }
	}
}
