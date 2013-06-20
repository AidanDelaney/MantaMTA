using System;
using System.Linq;
using System.Text.RegularExpressions;
using Colony101.MTA.Library.DNS;

namespace Colony101.MTA.Library.OutboundRules
{
	internal static class OutboundRuleManager
	{
		/// <summary>
		/// Holds a cached copy of the Outbound MX Patterns from the database.
		/// </summary>
		private static OutboundMxPatternCollection _MXPatterns { get; set; }
		/// <summary>
		/// Holds a cached copy of the Outbound Rules from the database.
		/// </summary>
		private static OutboundRuleCollection _Rules { get; set; }

		/// <summary>
		/// Gets the Outbound Rules for the specified destination MX / and IP Address.
		/// </summary>
		/// <param name="mxRecord">MXRecord for the destination MX.</param>
		/// <param name="mtaIpAddress">Outbound IP Address</param>
		/// <returns></returns>
		public static OutboundRuleCollection GetRules(MXRecord mxRecord, MtaIpAddress.MtaIpAddress mtaIpAddress)
		{
			// Get the data from the database. This needs to be cleaverer and reload every x minutes.
			if (_MXPatterns == null)
				_MXPatterns = DAL.OutboundRuleDB.GetOutboundRulePatterns();
			if (_Rules == null)
				_Rules = DAL.OutboundRuleDB.GetOutboundRules();

			/*
			 *	NEED SOME MAGIC HERE
			 *	Magic code should prevent us from having to do the below pattern matching everytime.
			 */

			// Loop through all of the patterns
			for (int i = 0; i < _MXPatterns.Count; i++)
			{
				// The current pattern were working with.
				OutboundMxPattern pattern = _MXPatterns[i];

				// If the patern applies only to a specified IP address then
				// only check for a match if getting rules for that IP.
				if (pattern.OutboundIpAddressID.HasValue)
				{
					if (pattern.OutboundIpAddressID.Value != mtaIpAddress.ID)
						continue;
				}

				if (pattern.Type == OutboundMxPatternType.CommaDelimited)
				{
					// Pattern is a comma delimited list, so split the values
					string[] strings = pattern.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					
					// Loop though the values in the split string array.
					for (int c = 0; c < strings.Length; c++)
					{
						// If they are a match return the rules.
						if (strings[i].Equals(mxRecord.Host, StringComparison.OrdinalIgnoreCase))
						{
							return new OutboundRuleCollection(from r
															  in _Rules
															  where r.OutboundMxPatternID == pattern.ID
															  select r);
						}
					}
				}
				else if (pattern.Type == OutboundMxPatternType.Regex)
				{
					// Pattern is Regex so just need to do an IsMatch
					if (Regex.IsMatch(mxRecord.Host, pattern.Value, RegexOptions.IgnoreCase))
					{
						// Found pattern match.
						return new OutboundRuleCollection(from r 
														  in _Rules 
														  where r.OutboundMxPatternID == pattern.ID
														  select r);
					}
					else
						continue;
				}
				else
				{
					// Don't know what to do with this pattern so move on to the next.
					Logging.Error("Unknown OutboundMxPatternType : " + pattern.Type.ToString());
					continue;
				}
			}

			// If we haven't returned rules at this point then we need.
			throw new Exception("No MX Rules found! Default Deleted?!?!");
		}
	}
}
