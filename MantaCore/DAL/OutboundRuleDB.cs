using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.OutboundRules;

namespace MantaMTA.Core.DAL
{
	internal static class OutboundRuleDB
	{
		/// <summary>
		/// Get the Outbound MX Patterns from the database.
		/// </summary>
		/// <returns></returns>
		internal static OutboundMxPatternCollection GetOutboundRulePatterns()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_rle_mxPattern
ORDER BY rle_mxPattern_id DESC"; // Order descending so default -1 is always at the bottom!

				return new OutboundMxPatternCollection(DataRetrieval.GetCollectionFromDatabase<OutboundMxPattern>(cmd, CreateAndFillOutboundMxPattern));
			}
		}

		/// <summary>
		/// Get the Outbound Rules from the database.
		/// </summary>
		/// <returns></returns>
		internal static OutboundRuleCollection GetOutboundRules()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_rle_rule";

				return new OutboundRuleCollection(DataRetrieval.GetCollectionFromDatabase<OutboundRule>(cmd, CreateAndFillOutboundRule));
			}
		}

		/// <summary>
		/// Create and fill an OutboundMxPattern object from the Data Record.
		/// </summary>
		/// <param name="record">Datarecord containing values for the new object.</param>
		/// <returns>OutboundMxPattern object.</returns>
		private static OutboundMxPattern CreateAndFillOutboundMxPattern(IDataRecord record)
		{
			OutboundMxPattern mxPattern = new OutboundMxPattern();

			mxPattern.Description = record.GetStringOrEmpty("rle_mxPattern_description");
			mxPattern.ID = record.GetInt32("rle_mxPattern_id");
			mxPattern.Name = record.GetString("rle_mxPattern_name");
			if (!record.IsDBNull("ip_ipAddress_id"))
				mxPattern.LimitedToOutboundIpAddressID = record.GetInt32("ip_ipAddress_id");
			mxPattern.Type = (OutboundMxPatternType)record.GetInt32("rle_patternType_id");
			mxPattern.Value = record.GetString("rle_mxPattern_value");
			return mxPattern;
		}

		/// <summary>
		/// Create and fill an OutboundRule object from the Data Record.
		/// </summary>
		/// <param name="record">Datarecord containing values for the new object.</param>
		/// <returns>OutboundRule object.</returns>
		private static OutboundRule CreateAndFillOutboundRule(IDataRecord record)
		{
			OutboundRule rule = new OutboundRule(record.GetInt32("rle_mxPattern_id"), (OutboundRuleType)record.GetInt32("rle_ruleType_id"), record.GetString("rle_rule_value"));
			
			return rule;
		}
	}
}
