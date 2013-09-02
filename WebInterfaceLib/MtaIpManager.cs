using MantaMTA.Core.DAL;
using MantaMTA.Core.MtaIpAddress;

namespace WebInterfaceLib
{
	public static class MtaIpManager
	{
		public static MtaIPGroupCollection GetAllIpGroups()
		{
			MtaIPGroupCollection ipGroups = MtaIpGroupDB.GetMtaIpGroups();
			foreach (MtaIPGroup grp in ipGroups)
			{
				grp.IpAddresses = MtaIpAddressDB.GetMtaIpGroupIps(grp.ID);
			}

			return ipGroups;
		}
	}
}
