using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class FeedbackLoopEmailAddressDB
	{
		/// <summary>
		/// Checks an address to see if it appears in the list of feedback loop addresses.
		/// </summary>
		/// <param name="address">Address to check.</param>
		/// <returns>TRUE if exists, FALSE if not.</returns>
		internal static bool IsFeedbackLoopEmailAddress(string address)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT 1 
FROM man_mta_fblAddress
WHERE mta_fblAddress_address = @address";
				cmd.Parameters.AddWithValue("@address", address);
				conn.Open();
				object result = cmd.ExecuteScalar();
				if (result == null)
					return false;

				return true;
			}
		}
	}
}
