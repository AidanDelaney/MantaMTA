using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using MantaMTA.Core.Events.Enums;

namespace MantaMTA.Core.Events
{
	internal class BounceRulesManager
	{
		/// <summary>
		/// Holds a singleton instance of the BounceRulesManager.
		/// </summary>
		public static BounceRulesManager Instance { get { return _Instance; } }
		private static readonly BounceRulesManager _Instance = new BounceRulesManager();
		private BounceRulesManager() { }

		private static BounceRulesCollection _bounceRules = null;
		public static BounceRulesCollection BounceRules
		{
			get
			{
				if (_bounceRules == null || _bounceRules.LoadedTimestampUtc.AddMinutes(5) < DateTime.UtcNow)
				{
					// Would be nice to write to a log that we're updating.
					_bounceRules = DAL.CfgBounceRules.GetBounceRules();
				}

				return _bounceRules;
			}
		}
	}

	/// <summary>
	/// Holds details of a Bounce Rule used to interpret a failed delivery message (could be an email or an SMTP response).
	/// </summary>
	internal class BounceRule
	{
		public int RuleID { get; set; }
		public BounceRuleType Type { get; set; }
		public string Criteria { get; set; }

	}


	internal class BounceRulesCollection : ConcurrentBag<BounceRule>
	{
		/// <summary>
		/// When the BounceRules were last loaded into this collection.
		/// If this is "too old", the collection will reload them to ensure configuration changes are used.
		/// </summary>
		public DateTime LoadedTimestampUtc { get; set; }

		public BounceRulesCollection()
		{

		}
	}
}
