using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebInterface.Models;
using MantaMTA.Core.DAL;
using MantaMTA.Core.OutboundRules;
using WebInterfaceLib;
using MantaMTA.Core.VirtualMta;

namespace WebInterface.Controllers
{
    public class OutboundRulesController : Controller
    {
        //
        // GET: /OutboundRules/
        public ActionResult Index()
        {
			return View(OutboundRuleDB.GetOutboundRulePatterns());
        }

		//
		// GET: /OutboundRules/Edit?id=
		public ActionResult Edit(int id = WebInterfaceParameters.OUTBOUND_RULES_NEW_PATTERN_ID)
		{
			OutboundMxPattern pattern = null;
			OutboundRuleCollection rules = null;

			if (id != WebInterfaceParameters.OUTBOUND_RULES_NEW_PATTERN_ID)
			{
				pattern = OutboundRuleDB.GetOutboundRulePatterns().Single(p => p.ID == id);
				rules = new OutboundRuleCollection(OutboundRuleDB.GetOutboundRules().Where(r => r.OutboundMxPatternID == id).ToArray());
			}
			else
			{
				pattern = new OutboundMxPattern();
				rules = new OutboundRuleCollection(OutboundRuleDB.GetOutboundRules().Where(r => r.OutboundMxPatternID == MantaMTA.Core.MtaParameters.OUTBOUND_RULES_DEFAULT_PATTERN_ID));
			}

			
			VirtualMTACollection vMtas = MantaMTA.Core.DAL.VirtualMtaDB.GetVirtualMtas();
			return View(new OutboundRuleModel(rules, pattern, vMtas));
		}

		//
		// GET: /OutboundRules/Delete?patternID=
		public ActionResult Delete(int patternID)
		{
			OutboundRuleWebManager.Delete(patternID);
			return View();
		}
    }
}
