using MantaMTA.Core.OutboundRules;

namespace WebInterfaceLib
{
	public static class OutboundRuleWebManager
	{
		/// <summary>
		/// Creates a new MX Pattern and it's default rules.
		/// </summary>
		/// <param name="name">Name of the pattern.</param>
		/// <param name="description">Description of the pattern.</param>
		/// <param name="type">The Type of the pattern.</param>
		/// <param name="pattern">The pattern value.</param>
		/// <param name="ipAddress">IP Address to limit the outbound rule to, or null if applies to all IP's.</param>
		/// <returns>ID of the MX Pattern.</returns>
		public static int CreatePattern(string name, string description, OutboundMxPatternType type, string pattern, int? ipAddress)
		{
			OutboundMxPattern mxPattern = new OutboundMxPattern
			{
				Description = description,
				Name = name,
				Type = type,
				Value = pattern,
				LimitedToOutboundIpAddressID = ipAddress
			};

			mxPattern.ID = Save(mxPattern);
			
			// Create the three types of rule.
			Save(new OutboundRule(mxPattern.ID, OutboundRuleType.MaxConnections, "-1"));
			Save(new OutboundRule(mxPattern.ID, OutboundRuleType.MaxMessagesConnection, "-1"));
			Save(new OutboundRule(mxPattern.ID, OutboundRuleType.MaxMessagesPerHour, "-1"));

			return mxPattern.ID;
		}

		/// <summary>
		/// Deletes the specified Outbound Rule pattern and all of it's rules.
		/// </summary>
		/// <param name="mxPatternID">ID of the MX Pattern to delete.</param>
		public static void Delete(int mxPatternID)
		{
			DAL.OutboundRulesDB.Delete(mxPatternID);
		}

		/// <summary>
		/// Saves the OutboundMxPattern.
		/// </summary>
		/// <param name="pattern">Pattern to save.</param>
		/// <returns>ID of the pattern.</returns>
		public static int Save(OutboundMxPattern pattern)
		{
			return DAL.OutboundRulesDB.Save(pattern);
		}

		/// <summary>
		/// Saves the OutboundRule.
		/// </summary>
		/// <param name="rule">The rule to save.</param>
		public static void Save(OutboundRule rule)
		{
			DAL.OutboundRulesDB.Save(rule);
		}
	}
}
