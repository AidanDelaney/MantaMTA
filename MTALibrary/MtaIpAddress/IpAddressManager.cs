using System;
using System.Linq;

namespace Colony101.MTA.Library.MtaIpAddress
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
		/// Method will load IP addresses from the database if required.
		/// This method should be called before doing anything with the 
		/// private IP collections.
		/// </summary>
		private static void LoadIpAddresses()
		{
			if (_ipAddresses != null &&
				_lastGotIpAddresses.AddMinutes(5) > DateTime.Now)
			{
				Console.Write("IP addresses already loaded");
				return;
			}

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
		/// Gets the specfied MTA IP Group
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static MtaIPGroup GetMtaIPGroup(int id)
		{
			MtaIPGroup group = DAL.MtaIpGroupDB.GetMtaIpGroup(id);
			group.IpAddresses = DAL.MtaIpAddressDB.GetMtaIpGroupIps(id);
			return group;
		}
	}
}
