using System;
using System.Data.SqlClient;
using MantaMTA.Core.DAL;

namespace WebInterfaceLib.DAL
{
	internal static class OutboundRulesDB
	{
		/// <summary>
		/// Deletes the MX Pattern and its rules from the database.
		/// </summary>
		/// <param name="patternID">ID of the pattern to delete.</param>
		public static void Delete(int mxPatternID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
DELETE FROM man_rle_mxPattern WHERE rle_mxPattern_id = @mxPatternID
DELETE FROM man_rle_rule WHERE rle_mxPattern_id = @mxPatternID
";
				cmd.Parameters.AddWithValue("@mxPatternID", mxPatternID);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Saves the OutboundRule to the database.
		/// </summary>
		/// <param name="outboundRule">The OutboundRule to save.</param>
		public static void Save(MantaMTA.Core.OutboundRules.OutboundRule outboundRule)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM man_rle_rule WHERE rle_mxPattern_id = @mxPatternID AND rle_ruleType_id = @type)
	BEGIN
		UPDATE man_rle_rule
		SET rle_rule_value = @value
	END
ELSE
	BEGIN
		INSERT INTO man_rle_rule(rle_mxPattern_id, rle_ruleType_id, rle_rule_value)
		VALUES(@mxPatternID, @type, @value)
	END
";
				cmd.Parameters.AddWithValue("@mxPatternID", outboundRule.OutboundMxPatternID);
				cmd.Parameters.AddWithValue("@type", (int)outboundRule.Type);
				cmd.Parameters.AddWithValue("@value", outboundRule.Value);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Saves the specified OutboundMxPattern to the database.
		/// </summary>
		/// <param name="mxPattern">The OutboundMxPattern to save.</param>
		/// <returns>ID of the OutboundMxPattern.</returns>
		public static int Save(MantaMTA.Core.OutboundRules.OutboundMxPattern mxPattern)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM man_rle_mxPattern WHERE rle_mxPattern_id = @mxPatternID)
	BEGIN
		UPDATE man_rle_mxPattern
		SET rle_mxPattern_name = @name,
		rle_mxPattern_description = @description,
		rle_patternType_id = @type,
		rle_mxPattern_value = @value,
		ip_ipAddress_id = @ipAddressID
		WHERE rle_mxPattern_id = @mxPatternID

		SELECT @mxPatternID
	END
ELSE
	BEGIN
		INSERT INTO man_rle_mxPattern(rle_mxPattern_name, rle_mxPattern_description, rle_patternType_id, rle_mxPattern_value, ip_ipAddress_id)
		VALUES(@name, @description, @type, @value, @ipAddressID)

		SELECT @@IDENTITY
	END
";
				cmd.Parameters.AddWithValue("@mxPatternID", mxPattern.ID);
				cmd.Parameters.AddWithValue("@name", mxPattern.Name);
				cmd.Parameters.AddWithValue("@description", mxPattern.Description);
				cmd.Parameters.AddWithValue("@type", (int)mxPattern.Type);
				cmd.Parameters.AddWithValue("@value", mxPattern.Value);
				if (mxPattern.LimitedToOutboundIpAddressID.HasValue)
					cmd.Parameters.AddWithValue("@ipAddressID", mxPattern.LimitedToOutboundIpAddressID.Value);
				else
					cmd.Parameters.AddWithValue("@ipAddressID", DBNull.Value);
				conn.Open();
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}
	}
}
