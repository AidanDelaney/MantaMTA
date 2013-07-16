using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MantaMTA.Core.MtaIpAddress
{
	/// <summary>
	/// Manager for IP addresses.
	/// Has own cache (5 min) so the database doesn't need to be hit for every message.
	/// </summary>
	public static class IpAddressManager
	{
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA.
		/// </summary>
		private static MtaIpAddressCollection _ipAddresses = null;
		
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA for sending.
		/// </summary>
		private static MtaIpAddressCollection _outboundIpAddresses = null;
		
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA to receive mail.
		/// </summary>
		private static MtaIpAddressCollection _inboundIpAddresses = null;
		
		/// <summary>
		/// Timestamp of when the _ipAddresses collection was filled.
		/// </summary>
		private static DateTime _lastGotIpAddresses = DateTime.MinValue;

		/// <summary>
		/// Collection of cached MtaIPGroupCached.
		/// </summary>
		private static ConcurrentDictionary<int, MtaIPGroup> _ipGroups = new ConcurrentDictionary<int, MtaIPGroup>();

		/// <summary>
		/// Method will load IP addresses from the database if required.
		/// This method should be called before doing anything with the 
		/// private IP collections.
		/// </summary>
		private static void LoadIpAddresses()
		{
			if (_ipAddresses != null &&
				_lastGotIpAddresses.AddMinutes(MtaParameters.MTA_CACHE_MINUTES) > DateTime.UtcNow)
				return;

			_outboundIpAddresses = null;
			_inboundIpAddresses = null;
			_ipAddresses = DAL.MtaIpAddressDB.GetMtaIpAddresses();
		}

		/// <summary>
		/// Returns a collection of IP addresses that should be
		/// used by the MTA for receiving messages.
		/// </summary>
		/// <returns></returns>
		public static MtaIpAddressCollection GetIPsForListeningOn()
		{
			LoadIpAddresses();
			
			if (_inboundIpAddresses == null)
				_inboundIpAddresses = new MtaIpAddressCollection(from ip
										   in _ipAddresses
										   where ip.IsSmtpInbound
										   select ip);

			return _inboundIpAddresses;
		}

		/// <summary>
		/// Gets a collection of IP address that can be used
		/// by the MTA for sending of messages.
		/// </summary>
		/// <returns></returns>
		public static MtaIpAddressCollection GetIPsForSending()
		{
			LoadIpAddresses();

			if (_outboundIpAddresses == null)
				_outboundIpAddresses = new MtaIpAddressCollection(from ip
										   in _ipAddresses
										   where ip.IsSmtpOutbound
										   select ip);

			return _outboundIpAddresses;
		}

		/// <summary>
		/// Gets the default MTA IP Group.
		/// </summary>
		/// <returns></returns>
		public static MtaIPGroup GetDefaultMtaIPGroup()
		{
			int defaultGroupID = DAL.CfgPara.GetDefaultIPGroupID();
			return GetMtaIPGroup(defaultGroupID);
		}

		/// <summary>
		/// Object used to lock inside the GetMtaIPGroup method.
		/// </summary>
		private static object _MtaIPGroupSyncLock = new object();

		/// <summary>
		/// Gets the specfied MTA IP Group
		/// </summary>
		/// <param name="id">ID of the group to get.</param>
		/// <returns>The IP Group or NULL if doesn't exist.</returns>
		public static MtaIPGroup GetMtaIPGroup(int id)
		{
			MtaIPGroup group = null;

			// Try and get IPGroup from the in memory collection.
			if (_ipGroups.TryGetValue(id, out group))
			{
				// Only cache IP Groups for N minutes.
				if (group.CreatedTimestamp.AddMinutes(MtaParameters.MTA_CACHE_MINUTES) > DateTime.UtcNow)
					return group;
			}

			// We need to goto the database to get the group. Lock!
			lock (_MtaIPGroupSyncLock)
			{
				// Check that something else didn't already load from the database.
				// If it did then we can just return that.
				_ipGroups.TryGetValue(id, out group);
				if (group != null)
					return group;

				// Get group from the database.
				group = DAL.MtaIpGroupDB.GetMtaIpGroup(id);

				// Group doesn't exist, so don't try and get it's IPs
				if (group == null)
					return null;

				// Got the group, go get it's IPs.
				group.IpAddresses = DAL.MtaIpAddressDB.GetMtaIpGroupIps(id);

				// Add the group to collection, so others can use it.
				_ipGroups.TryAdd(id, group);
				return group;
			}
		}
	}
}
