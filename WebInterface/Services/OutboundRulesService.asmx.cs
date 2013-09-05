using System;
using System.Web.Script.Services;
using System.Web.Services;
using MantaMTA.Core.OutboundRules;
using WebInterfaceLib;
using System.Linq;

namespace WebInterface.Services
{
	/// <summary>
	/// Summary description for OutboundRulesService
	/// </summary>
	[WebService(Namespace = "http://manta.io/mantamta/web")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[ScriptService] 
	public class OutboundRulesService : System.Web.Services.WebService
	{

		[WebMethod]
		public bool Update(int patternID, string name, string description, int? virtualMTA, OutboundMxPatternType type, string patternValue, int maxConnections, int maxMessagesConn, int maxMessagesHour)
		{
			if (virtualMTA == -1)
				virtualMTA = null;

			OutboundMxPattern pattern = null;
			if (patternID == WebInterfaceLib.WebInterfaceParameters.OUTBOUND_RULES_NEW_PATTERN_ID)
				pattern = new OutboundMxPattern();
			else
				pattern = MantaMTA.Core.DAL.OutboundRuleDB.GetOutboundRulePatterns().SingleOrDefault(p => p.ID == patternID);
			if (pattern == null)
				return false;

			pattern.Description = description.Trim();
			pattern.LimitedToOutboundIpAddressID = virtualMTA;
			pattern.Name = name.Trim();
			pattern.Type = type;
			pattern.Value = patternValue;
			pattern.ID = OutboundRuleWebManager.Save(pattern);

			OutboundRuleWebManager.Save(new OutboundRule(pattern.ID, OutboundRuleType.MaxConnections, maxConnections.ToString()));
			OutboundRuleWebManager.Save(new OutboundRule(pattern.ID, OutboundRuleType.MaxMessagesConnection, maxMessagesConn.ToString()));
			OutboundRuleWebManager.Save(new OutboundRule(pattern.ID, OutboundRuleType.MaxMessagesPerHour, maxMessagesHour.ToString()));

			return true;
		}
	}
}
