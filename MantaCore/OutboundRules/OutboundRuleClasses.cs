using System.Collections.Generic;

namespace MantaMTA.Core.OutboundRules
{
	/// <summary>
	/// Identifies a Type of outbound rule.
	/// </summary>
	public enum OutboundRuleType : int
	{
		/// <summary>
		/// Rule holds the maximum simultaneous connections value.
		/// </summary>
		MaxConnections = 1,
		/// <summary>
		/// Rule holds the maximum messages per connections value.
		/// </summary>
		MaxMessagesConnection = 2,
		/// <summary>
		/// Rule holds the maximum messages per hour, all connections.
		/// </summary>
		MaxMessagesPerHour = 3
	}

	/// <summary>
	/// Identifies the type of pattern to match with.
	/// </summary>
	public enum OutboundMxPatternType : int
	{
		/// <summary>
		/// Value is a regular expression.
		/// </summary>
		Regex = 1,
		/// <summary>
		/// Value is a comma delimited list of string to equals.
		/// </summary>
		CommaDelimited = 2
	}

	/// <summary>
	/// Holds an outbound MX pattern, this is used to match against
	/// an MX servers host name in it's MX record.
	/// </summary>
	public class OutboundMxPattern
	{
		/// <summary>
		/// ID of this pattern.
		/// </summary>
		public int ID { get; set; }
		
		/// <summary>
		/// Name of this pattern.
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Description of this pattern.
		/// </summary>
		public string Description { get; set; }
		
		/// <summary>
		/// The type of this pattern.
		/// </summary>
		public OutboundMxPatternType Type { get; set; }

		/// <summary>
		/// The value to use for matching the MX Record hostname.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// If has value, only apply this pattern against sending from
		/// specified IP address.
		/// </summary>
		public int? LimitedToOutboundIpAddressID { get; set; }

		public OutboundMxPattern()
		{
			this.ID = 0;
			this.Name = string.Empty;
			this.Description = string.Empty;

			// Default to Regex match all.
			this.Type = OutboundMxPatternType.Regex;
			this.Value = ".";
			this.LimitedToOutboundIpAddressID = null;
		}
	}

	/// <summary>
	/// Holds a rule for outbound clients.
	/// </summary>
	public class OutboundRule
	{
		/// <summary>
		/// ID of the pattern that this rule should be used with.
		/// </summary>
		public int OutboundMxPatternID { get; set; }

		/// <summary>
		/// Identifies the type of this rule.
		/// </summary>
		public OutboundRuleType Type { get; set; }

		/// <summary>
		/// The value of this rule.
		/// </summary>
		public string Value { get; set; }

		public OutboundRule(int outbounbMxPatternID, OutboundRuleType type, string value)
		{
			this.OutboundMxPatternID = outbounbMxPatternID;
			this.Type = type;
			this.Value = value;
		}
	}

	/// <summary>
	/// Holds a collection of Outbound Rules.
	/// </summary>
	public class OutboundRuleCollection : List<OutboundRule>
	{
		public OutboundRuleCollection() { }
		public OutboundRuleCollection(IEnumerable<OutboundRule> collection) : base(collection) { }
	}

	/// <summary>
	/// Holds a collection of OutboundMxPatterns.
	/// </summary>
	public class  OutboundMxPatternCollection : List<OutboundMxPattern>
	{
		public OutboundMxPatternCollection() { }
		public OutboundMxPatternCollection(IEnumerable<OutboundMxPattern> collection) : base(collection) { }
	}
}
