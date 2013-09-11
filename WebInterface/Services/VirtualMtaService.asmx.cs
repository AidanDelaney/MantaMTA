using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using MantaMTA.Core.VirtualMta;
using WebInterfaceLib;
using WebInterfaceLib.DAL;

namespace WebInterface.Services
{
	/// <summary>
	/// Summary description for VirtualMtaService
	/// </summary>
	[WebService(Namespace = "http://manta.io/mantamta/web")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[ScriptService]
	public class VirtualMtaService : System.Web.Services.WebService
	{
		/// <summary>
		/// Updates an existing Virtual MTA.
		/// </summary>
		/// <param name="id">ID of the virtual MTA.</param>
		/// <param name="hostname">Hostname of the Virtual MTA.</param>
		/// <param name="ipAddress">IP Address of the Virtual MTA.</param>
		/// <param name="inbound">TRUE if the Virtual MTA can accept inbound Email.</param>
		/// <param name="outbound">TRUE if the Virtal MTA can send outbound Email.</param>
		/// <returns>TRUE if updated or FALSE if update failed.</returns>
		[WebMethod]
		public bool Save(int id, string hostname, string ipAddress, bool inbound, bool outbound)
		{
			VirtualMTA vMTA = null;
			
			if (id != WebInterfaceParameters.VIRTUALMTA_NEW_ID)
				vMTA = MantaMTA.Core.DAL.VirtualMtaDB.GetVirtualMta(id);
			else
				vMTA = new VirtualMTA();

			if (vMTA == null)
				return false;

			if (string.IsNullOrWhiteSpace(hostname))
				return false;

			IPAddress ip = null;
			try
			{
				ip = IPAddress.Parse(ipAddress);
			}
			catch(Exception)
			{
				return false;
			}

			vMTA.Hostname = hostname;
			vMTA.IPAddress = ip;
			vMTA.IsSmtpInbound = inbound;
			vMTA.IsSmtpOutbound = outbound;
			VirtualMtaDB.Save(vMTA);
			return true;
		}

		/// <summary>
		/// Deletes the specified Virtual MTA.
		/// </summary>
		/// <param name="id">ID of the Virtual MTA to delete.</param>
		[WebMethod]
		public void Delete(int id)
		{
			VirtualMtaDB.Delete(id);
		}

		/// <summary>
		/// Saves the Virtual MTA Group.
		/// </summary>
		/// <param name="id">ID of the Virtual MTA Group to save.</param>
		/// <param name="name">Name of the Virtual MTA Group.</param>
		/// <param name="description">Description of the Virtual MTA Group.</param>
		/// <param name="mtaIDs">ID's of the VirtualMTAs that the Group should contain.</param>
		/// <returns>TRUE if saved or FALSE if not saved.</returns>
		[WebMethod]
		public bool SaveGroup(int id, string name, string description, int[] mtaIDs)
		{
			VirtualMtaGroup grp = null;
			if (id == WebInterfaceParameters.VIRTUALMTAGROUP_NEW_ID)
				grp = new VirtualMtaGroup();
			else
				grp = MantaMTA.Core.DAL.VirtualMtaGroupDB.GetVirtualMtaGroup(id);

			if (grp == null)
				return false;

			grp.Name = name;
			grp.Description = description;

			VirtualMTACollection vMtas = MantaMTA.Core.DAL.VirtualMtaDB.GetVirtualMtas();
			for (int i = 0; i < mtaIDs.Length; i++)
			{
				VirtualMTA mta = vMtas.SingleOrDefault(m => m.ID == mtaIDs[i]);
				if (mta == null)
					return false;
				grp.VirtualMtaCollection.Add(mta);
			}

			VirtualMtaWebManager.Save(grp);
			return true;
		}

		/// <summary>
		/// Deletes a Virtual MTA Group.
		/// </summary>
		/// <param name="id">ID of the Virtual MTA Group to delete.</param>
		[WebMethod]
		public void DeleteGroup(int id)
		{
			VirtualMtaWebManager.DeleteGroup(id);
		}
	}
}
