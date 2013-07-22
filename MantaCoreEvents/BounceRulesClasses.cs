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
		/// <summary>
		/// The unique ID of the Rule.  Allows updating of existing Rules through an interface.
		/// </summary>
		public int RuleID { get; set; }
		/// <summary>
		/// The Name of the Rule to give it some meaning to a user.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// An optional descriptive piece of text to explain what the purpose of the Rule is.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Used to put the Rules in order as some may need to be tested before others.
		/// </summary>
		public int ExecutionOrder { get; set; }
		/// <summary>
		/// Indicates whether a Rule is part of the system or user-created.
		///		true: part of the system so shouldn't be edited by a user.
		///		false: has been created by a user so can be edited or deleted.
		/// </summary>
		public bool IsBuiltIn { get; set; }
		/// <summary>
		/// How to perform testing of the Rule: is it a Regex pattern or a simple string match?
		/// </summary>
		public BounceRuleCriteriaType CriteriaType { get; set; }
		/// <summary>
		/// The text to use as the criteria.  The value of <paramref name="CriteriaType"/> indicates
		/// how the Criteria should be tested.
		/// </summary>
		public string Criteria { get; set; }
		/// <summary>
		/// The MantaBounceType to be used when this Rule matches.
		/// </summary>
		public MantaBounceType BounceTypeIndicated { get; set; }
		/// <summary>
		/// The MantaBounceCode to be used when this Rule matches.
		/// </summary>
		public MantaBounceCode BounceCodeIndicated { get; set; }
		/// <summary>
		/// The number of times this Rule has resulted in a match.
		/// </summary>
		public int Hits { get; set; }


		/// <summary>
		/// Constructor.
		/// </summary>
		public BounceRule()
		{
			this.Hits = 0;
		}


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
					Match m = Regex.Match(message, this.Criteria, RegexOptions.Multiline | RegexOptions.IgnoreCase);

					if (m.Success)
					{
						this.Hits++;

						// Regex patterns that match to the end of a line may contain a "\r" at the end as
						// "$" matches _between_ a "\r" and a "\n".
						matchedMessage = m.Value.Trim();

						return true;
					}
					break;

				case BounceRuleCriteriaType.StringMatch:
					if (message.IndexOf(this.Criteria, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						this.Hits++;
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


	/// <summary>
	/// Holds a collection of BounceRule objects.
	/// </summary>
	internal class BounceRulesCollection : List<BounceRule>
	{
		/// <summary>
		/// When the BounceRules were last loaded into this collection.
		/// If this is "too old", the collection will reload them to ensure configuration changes are used.
		/// </summary>
		public DateTime LoadedTimestampUtc { get; set; }

		/// <summary>
		/// Standard constructor for a BounceRulesCollection.
		/// </summary>
		public BounceRulesCollection() : base() { }

		/// <summary>
		/// Allows copying of a BounceRulesCollection or the creation of one from a collection of Rules,
		/// e.g. a List&lt;BounceRule&gt;.
		/// </summary>
		/// <param name="collection"></param>
		public BounceRulesCollection(IEnumerable<BounceRule> collection) : base(collection) { }
	}
}
