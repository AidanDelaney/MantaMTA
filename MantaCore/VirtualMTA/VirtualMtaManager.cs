using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MantaMTA.Core.VirtualMta
{
	/// <summary>
	/// Manager for VirtualMTAs.
	/// Has own cache (5 min) so the database doesn't need to be hit for every message.
	/// </summary>
	public static class VirtualMtaManager
	{
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA.
		/// </summary>
		private static VirtualMTACollection _vmtaCollection = null;
		
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA for sending.
		/// </summary>
		private static VirtualMTACollection _outboundMtas = null;
		
		/// <summary>
		/// Collection of the IP addresses that can be used by the MTA to receive mail.
		/// </summary>
		private static VirtualMTACollection _inboundMtas = null;
		
		/// <summary>
		/// Timestamp of when the _ipAddresses collection was filled.
		/// </summary>
		private static DateTime _lastGotVirtualMtas = DateTime.MinValue;

		/// <summary>
		/// Collection of cached MtaIPGroupCached.
		/// </summary>
		private static ConcurrentDictionary<int, VirtualMtaGroup> _vmtaGroups = new ConcurrentDictionary<int, VirtualMtaGroup>();

		/// <summary>
		/// Method will load IP addresses from the database if required.
		/// This method should be called before doing anything with the 
		/// private IP collections.
		/// </summary>
		private static void LoadVirtualMtas()
		{
			if (_vmtaCollection != null &&
				_lastGotVirtualMtas.AddMinutes(MtaParameters.MTA_CACHE_MINUTES) > DateTime.UtcNow)
				return;

			_outboundMtas = null;
			_inboundMtas = null;
			_vmtaCollection = DAL.VirtualMtaDB.GetVirtualMtas();
		}

		/// <summary>
		/// Returns a collection of IP addresses that should be
		/// used by the MTA for receiving messages.
		/// </summary>
		/// <returns></returns>
		public static VirtualMTACollection GetVirtualMtasForListeningOn()
		{
			LoadVirtualMtas();
			
			if (_inboundMtas == null)
				_inboundMtas = new VirtualMTACollection(from ip
										   in _vmtaCollection
										   where ip.IsSmtpInbound
										   select ip);

			return _inboundMtas;
		}

		/// <summary>
		/// Gets a collection of IP address that can be used
		/// by the MTA for sending of messages.
		/// </summary>
		/// <returns></returns>
		public static VirtualMTACollection GetVirtualMtasForSending()
		{
			LoadVirtualMtas();

			if (_outboundMtas == null)
				_outboundMtas = new VirtualMTACollection(from ip
										   in _vmtaCollection
										   where ip.IsSmtpOutbound
										   select ip);

			return _outboundMtas;
		}

		/// <summary>
		/// Gets the default MTA IP Group.
		/// </summary>
		/// <returns></returns>
		public static VirtualMtaGroup GetDefaultVirtualMtaGroup()
		{
			int defaultGroupID = DAL.CfgPara.GetDefaultIPGroupID();
			return GetVirtualMtaGroup(defaultGroupID);
		}

		/// <summary>
		/// Object used to lock inside the GetMtaIPGroup method.
		/// </summary>
		private static object _MtaVirtualMtaGroupSyncLock = new object();

		/// <summary>
		/// Gets the specfied MTA IP Group
		/// </summary>
		/// <param name="id">ID of the group to get.</param>
		/// <returns>The IP Group or NULL if doesn't exist.</returns>
		public static VirtualMtaGroup GetVirtualMtaGroup(int id)
		{
			VirtualMtaGroup group = null;

			// Try and get IPGroup from the in memory collection.
			if (_vmtaGroups.TryGetValue(id, out group))
			{
				// Only cache IP Groups for N minutes.
				if (group.CreatedTimestamp.AddMinutes(MtaParameters.MTA_CACHE_MINUTES) > DateTime.UtcNow)
					return group;
			}

			// We need to goto the database to get the group. Lock!
			lock (_MtaVirtualMtaGroupSyncLock)
			{
				// Check that something else didn't already load from the database.
				// If it did then we can just return that.
				_vmtaGroups.TryGetValue(id, out group);
				if (group != null && group.CreatedTimestamp.AddMinutes(MtaParameters.MTA_CACHE_MINUTES) > DateTime.UtcNow)
					return group;

				// Get group from the database.
				group = DAL.VirtualMtaGroupDB.GetVirtualMtaGroup(id);

				// Group doesn't exist, so don't try and get it's IPs
				if (group == null)
					return null;

				// Got the group, go get it's IPs.
				group.VirtualMtaCollection = DAL.VirtualMtaDB.GetVirtualMtasInVirtualMtaGroup(id);

				// Add the group to collection, so others can use it.
				_vmtaGroups.TryAdd(id, group);
				return group;
			}
		}
	}
}
