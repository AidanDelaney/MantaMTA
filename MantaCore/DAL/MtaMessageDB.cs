using System;
using System.Threading.Tasks;

namespace MantaMTA.Core.DAL
{
    internal static class MtaMessageDB
	{
		/// <summary>
		/// Delimiter user for RCPT addresses.
		/// </summary>
		private const string _RcptToDelimiter = ",";
        
        public static async Task<string> GetMailFrom(Guid messageId)
        {
            using (var conn = MantaDB.GetSqlConnection())
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT TOP 1 mta_msg_mailFrom
FROM man_mta_msg
WHERE mta_msg_id = @msgId";
                cmd.Parameters.AddWithValue("@msgId", messageId);
                await conn.OpenAsync().ConfigureAwait(false);
                var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
                if (result == null)
                    return string.Empty;
                return result.ToString();
            }
        }
	}
}
