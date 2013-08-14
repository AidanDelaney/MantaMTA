using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using MantaMTA.Core.DAL;
using System.Data;
using MantaMTA.Core.Events.Enums;

namespace MantaMTA.Core.Events.DAL
{
	/// <summary>
	/// Performs database querying and retrieval operations for Manta's Events.
	/// </summary>
	internal static class EventDB
	{
		/// <summary>
		/// Retrieves all BounceRules from the database.
		/// </summary>
		/// <returns>A BounceRulesCollection of all the Rules.</returns>
		internal static BounceRulesCollection GetBounceRules()
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_evn_bounceRule
ORDER BY evn_bounceRule_executionOrder ASC";
				return new BounceRulesCollection(DataRetrieval.GetCollectionFromDatabase<BounceRule>(cmd, CreateAndFillBounceRuleFromRecord));
			}
		}


		/// <summary>
		/// Create and fill a BounceRule object from the Data Record.
		/// </summary>
		/// <param name="record">Datarecord containing values for the new object.</param>
		/// <returns>A BounceRule object.</returns>
		private static BounceRule CreateAndFillBounceRuleFromRecord(IDataRecord record)
		{
			BounceRule rule = new BounceRule();

			rule.RuleID = record.GetInt32("evn_bounceRule_id");
			rule.Name = record.GetString("evn_bounceRule_name");
			rule.Description = record.GetStringOrEmpty("evn_bounceRule_description");
			rule.ExecutionOrder = record.GetInt32("evn_bounceRule_executionOrder");
			rule.IsBuiltIn = record.GetBoolean("evn_bounceRule_isBuiltIn");
			rule.CriteriaType = (BounceRuleCriteriaType)record.GetInt32("evn_bounceRuleCriteriaType_id");
			rule.Criteria = record.GetString("evn_bounceRule_criteria");
			rule.BounceTypeIndicated = (MantaBounceType)record.GetInt32("evn_bounceRule_mantaBounceType");
			rule.BounceCodeIndicated = (MantaBounceCode)record.GetInt32("evn_bounceRule_mantaBounceCode");

			return rule;
		}

	}
}
