using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MantaMTA.Core.Events.Enums;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// Holds details of a Bounce Rule used to interpret a failed delivery message (could be an email or an SMTP response).
	/// </summary>
	internal class BounceRule
	{
		public int RuleID { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public BounceRuleCriteriaType CriteriaType { get; set; }
		public string Criteria { get; set; }
		public MantaBounceType BounceTypeIndicated { get; set; }
		public MantaBounceCode BounceCodeIndicated { get; set; }

		/// <summary>
		/// Checks whether the Bounce Rule's criteria matches a supplied message.
		/// </summary>
		/// <param name="message">A message to check against this Bounce Rule.</param>
		/// <param name="matchedMessage">out parameter.  Used to return the message that matched the Bounce Rule.
		/// If the Bounce Rule didn't match <paramref name="message"/>, then this will be string.Empty.</param>
		/// <returns>true if the Rule matches, else false.</returns>
		public bool IsMatch(string message, out string matchedMessage)
		{
			switch (this.CriteriaType)
			{
				case BounceRuleCriteriaType.RegularExpressionPattern:
					Match m = Regex.Match(message, this.Criteria, RegexOptions.Multiline);

					if (m != null)
					{
						matchedMessage = m.Value;
						return true;
					}
					break;

				case BounceRuleCriteriaType.StringMatch:
					if (message.IndexOf(this.Criteria, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						matchedMessage = this.Criteria;
						return true;
					}

					break;

				case BounceRuleCriteriaType.Unknown:
				default:
					throw new ArgumentException("Unhandled BounceRuleCriteriaType \"" + this.CriteriaType.ToString() + "\".");
			}


			// If we fell through the switch without returning, we didn't find a match.
			matchedMessage = string.Empty;
			return false;
		}
	}


	internal class BounceRulesCollection : ConcurrentBag<BounceRule>
	{
		/// <summary>
		/// When the BounceRules were last loaded into this collection.
		/// If this is "too old", the collection will reload them to ensure configuration changes are used.
		/// </summary>
		public DateTime LoadedTimestampUtc { get; set; }
	}
}
