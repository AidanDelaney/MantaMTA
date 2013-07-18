using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using MantaMTA.Core.Events.Enums;
using System.Text.RegularExpressions;

namespace MantaMTA.Core.Events
{
	internal partial class BounceRulesManager
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
					_bounceRules = DAL.EventDB.GetBounceRules();

					// Ensure the Rules are in the correct order.
					_bounceRules = new BounceRulesCollection(_bounceRules.OrderBy(r => r.ExecutionOrder));
				}

				return _bounceRules;
			}
		}
	}
}
