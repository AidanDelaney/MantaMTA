using MantaMTA.Core.DAL;
using MantaMTA.Core.MtaIpAddress;

namespace WebInterfaceLib
{
	public static class MtaIpManager
	{
		/// <summary>
		/// Get a collection of all of the Virtual MTA Groups.
		/// </summary>
		/// <returns></returns>
		public static MtaIPGroupCollection GetAllIpGroups()
		{
			MtaIPGroupCollection ipGroups = MtaIpGroupDB.GetMtaIpGroups();
			
			// Get all the groups Virtual MTAs.
			foreach (MtaIPGroup grp in ipGroups)
			{
				grp.IpAddresses = MtaIpAddressDB.GetMtaIpGroupIps(grp.ID);
			}

			return ipGroups;
		}
	}
}
