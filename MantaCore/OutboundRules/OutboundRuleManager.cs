using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using MantaMTA.Core.DNS;

namespace MantaMTA.Core.OutboundRules
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
		/// Holds a cached collection of matched patterns.
		/// Key: IP Address tostring()
		/// </summary>
		private static ConcurrentDictionary<string, MatchedMxPatternCollection> _matchedPatterns { get; set; }

		/// <summary>
		/// Class represents a matched MX Pattern
		/// </summary>
		private class MatchedMxPattern
		{
			/// <summary>
			/// ID of the pattern that resulted in this match.
			/// </summary>
			public int MxPatternID { get; set; }
			/// <summary>
			/// IP Address if specific otherwise string.empty.
			/// </summary>
			public string IPAddress { get; set; }
			/// <summary>
			/// DateTime of the match.
			/// </summary>
			public DateTime MatchedUtc { get; set; }

			public MatchedMxPattern()
			{
				MxPatternID = -1;
				IPAddress = null;
			}
		}

		/// <summary>
		/// Holds a collection of matched MX patterns
		/// Key: MX Record hostname.
		/// </summary>
		private class MatchedMxPatternCollection : ConcurrentDictionary<string, MatchedMxPattern>
		{
			/// <summary>
			/// Adds or updates.
			/// </summary>
			/// <param name="mxPatternID">The matching pattern ID</param>
			/// <param name="ipAddress">IP Address if specific or NULL</param>
			public void Add(int mxPatternID, MtaIpAddress.MtaIpAddress ipAddress)
			{
				MatchedMxPattern newMxPattern = new MatchedMxPattern();
				newMxPattern.MatchedUtc = DateTime.UtcNow;
				newMxPattern.MxPatternID = mxPatternID;

				 Func<string, MatchedMxPattern, MatchedMxPattern> updateAction = new Func<string, MatchedMxPattern, MatchedMxPattern>(delegate(string key, MatchedMxPattern existing)
									{
										if (existing.MatchedUtc > newMxPattern.MatchedUtc)
											return existing;
										return newMxPattern;
									});

				if (ipAddress != null)
				{
					newMxPattern.IPAddress = ipAddress.IPAddress.ToString();
					this.AddOrUpdate(newMxPattern.IPAddress,
									 new MatchedMxPattern()
									 {
										 MatchedUtc = DateTime.UtcNow,
										 MxPatternID = mxPatternID
									 }, updateAction);
				}
				else
					this.AddOrUpdate(string.Empty,
									 new MatchedMxPattern()
									 {
										 MatchedUtc = DateTime.UtcNow,
										 MxPatternID = mxPatternID
									 }, updateAction);
			}

			/// <summary>
			/// Gets the matched MX Record. Null if not found.
			/// </summary>
			/// <param name="ipAddress"></param>
			/// <returns></returns>
			public MatchedMxPattern GetMatchedMxPattern(MtaIpAddress.MtaIpAddress ipAddress)
			{
				MatchedMxPattern tmp;
				if (this.TryGetValue(ipAddress.IPAddress.ToString(), out tmp))
				{
					if(tmp.MatchedUtc.AddMinutes(5) < DateTime.UtcNow)
						return tmp;
				}
				else
				{
					if (this.TryGetValue(string.Empty, out tmp))
					{
						if (tmp.MatchedUtc.AddMinutes(5) > DateTime.UtcNow)
							return tmp;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the Outbound Rules for the specified destination MX and optionally IP Address.
		/// </summary>
		/// <param name="mxRecord">MXRecord for the destination MX.</param>
		/// <param name="mtaIpAddress">Outbound IP Address</param>
		/// <param name="mxPatternID">OUT: the ID of MxPattern that caused match.</param>
		/// <returns></returns>
		public static OutboundRuleCollection GetRules(MXRecord mxRecord, MtaIpAddress.MtaIpAddress mtaIpAddress, out int mxPatternID)
		{
			// Get the data from the database. This needs to be cleverer and reload every x minutes.
			if (_MXPatterns == null)
				_MXPatterns = DAL.OutboundRuleDB.GetOutboundRulePatterns();
			if (_Rules == null)
				_Rules = DAL.OutboundRuleDB.GetOutboundRules();

			int patternID = GetMxPatternID(mxRecord, mtaIpAddress);
			mxPatternID = patternID;
			
			return new OutboundRuleCollection(from r
											  in _Rules
											  where r.OutboundMxPatternID == patternID
											  select r);
		}

		/// <summary>
		/// Gets the MxPatternID that matches the MX Record, Outbound IP Address combo.
		/// </summary>
		/// <param name="record"></param>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		private static int GetMxPatternID(MXRecord record, MtaIpAddress.MtaIpAddress ipAddress)
		{
			if (_matchedPatterns == null)
				_matchedPatterns = new ConcurrentDictionary<string,MatchedMxPatternCollection>();

			MatchedMxPatternCollection matchedPatterns = _matchedPatterns.GetOrAdd(record.Host, new MatchedMxPatternCollection());
			MatchedMxPattern matchedPattern = matchedPatterns.GetMatchedMxPattern(ipAddress);

			if (matchedPattern != null &&
				matchedPattern.MatchedUtc.AddMinutes(5) > DateTime.UtcNow)
				// Found a valid cached pattern ID so return it.
				return matchedPattern.MxPatternID;

			// Loop through all of the patterns
			for (int i = 0; i < _MXPatterns.Count; i++)
			{
				// The current pattern we're working with.
				OutboundMxPattern pattern = _MXPatterns[i];

				// If the pattern applies only to a specified IP address then
				// only check for a match if getting rules for that IP.
				if (pattern.LimitedToOutboundIpAddressID.HasValue)
				{
					if (pattern.LimitedToOutboundIpAddressID.Value != ipAddress.ID)
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
						if (strings[i].Equals(record.Host, StringComparison.OrdinalIgnoreCase))
						{
							if (pattern.LimitedToOutboundIpAddressID.HasValue)
								matchedPatterns.Add(pattern.ID, ipAddress);
							else
								matchedPatterns.Add(pattern.ID, null);
							
							return pattern.ID;
						}
					}

					continue;
				}
				else if (pattern.Type == OutboundMxPatternType.Regex)
				{
					// Pattern is Regex so just need to do an IsMatch
					if (Regex.IsMatch(record.Host, pattern.Value, RegexOptions.IgnoreCase))
					{
						// Found pattern match.
						if (pattern.LimitedToOutboundIpAddressID.HasValue)
							matchedPatterns.Add(pattern.ID, ipAddress);
						else
							matchedPatterns.Add(pattern.ID, null);

						return pattern.ID;
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

			// Should have been found by default at least, but hasn't.
			Logging.Fatal("No MX Pattern Rules! Default Deleted?");
			Environment.Exit(0);
			return -1;
		}

		/// <summary>
		/// Gets the MAX number of messages allowed to be sent through the connection.
		/// </summary>
		/// <param name="record">MX Record for the destination.</param>
		/// <param name="ipAddress">IPAddress that we are sending from.</param>
		/// <returns>Max number of messages per connection.</returns>
		public static int GetMaxMessagesPerConnection(MXRecord record, MtaIpAddress.MtaIpAddress ipAddress)
		{
			int mxPatternID = 0;
			OutboundRuleCollection rules = GetRules(record, ipAddress, out mxPatternID);
			for (int i = 0; i < rules.Count; i++)
			{
				if (rules[i].Type == OutboundRuleType.MaxMessagesConnection)
				{
					int tmp = 0;
					if (int.TryParse(rules[i].Value, out tmp))
						return tmp;
					else
					{
						Logging.Error("Failed to get max messages per connection for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " value wasn't valid [" + rules[i].Value + "], defaulting to 1");
						return 1;
					}
				}
			}

			Logging.Error("Failed to get max messages per connection for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " defaulting to 1");
			return 1;
		}

		/// <summary>
		/// Gets the maximum amount of messages to send per hour from each ip address to mx.
		/// </summary>
		/// <param name="ipAddress">Outbound IP address</param>
		/// <param name="record">MX Record of destination server.</param>
		/// <param name="mxPatternID">ID of the pattern used to identify the rule.</param>
		/// <returns>Maximum number of messages per hour or -1 for unlimited.</returns>
		public static int GetMaxMessagesDestinationHour(MtaIpAddress.MtaIpAddress ipAddress, MXRecord record, out int mxPatternID)
		{
			OutboundRuleCollection rules = GetRules(record, ipAddress, out mxPatternID);
			for (int i = 0; i < rules.Count; i++)
			{
				if (rules[i].Type == OutboundRuleType.MaxMessagesPerHour)
				{
					int tmp = 0;
					if (int.TryParse(rules[i].Value, out tmp))
						return tmp;
					else
					{
						Logging.Error("Failed to get max messages per hour for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " value wasn't valid [" + rules[i].Value + "], defaulting to unlimited");
						return -1;
					}
				}
			}

			Logging.Error("Failed to get max messages per hour for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " defaulting to unlimited");
			return -1;
		}

		/// <summary>
		/// Gets the maximum amount of simultaneous connections to specified host.
		/// </summary>
		/// <param name="ipAddress">IP Address connecting from.</param>
		/// <param name="record">MXRecord of the destination.</param>
		/// <returns>Max number of connections.</returns>
		internal static int GetMaxConnectionsToDestination(MtaIpAddress.MtaIpAddress ipAddress, MXRecord record)
		{
			int mxPatternID = -1;
			OutboundRuleCollection rules = GetRules(record, ipAddress, out mxPatternID);
			for (int i = 0; i < rules.Count; i++)
			{
				if (rules[i].Type == OutboundRuleType.MaxConnections)
				{
					int tmp = 0;
					if (int.TryParse(rules[i].Value, out tmp))
						return tmp;
					else
					{
						Logging.Error("Failed to get max connections for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " value wasn't valid [" + rules[i].Value + "], defaulting to 1");
						return 1;
					}
				}
			}

			Logging.Error("Failed to get max connections for " + record.Host + " using " + ipAddress.IPAddress.ToString() + " defaulting to 1");
			return 1;
		}
	}
}
