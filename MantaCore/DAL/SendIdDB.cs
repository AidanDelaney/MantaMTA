using System.Configuration;
using System.Data.SqlClient;

namespace MantaMTA.Core.DAL
{
	internal static class SendIdDB
	{
		/// <summary>
		/// Gets the sendID's internal ID from the database. If the record doesn't exist
		/// then it will be created.
		/// </summary>
		/// <param name="sendID">The SendID to get the internal ID for.</param>
		/// <returns></returns>
		public static int CreateAndGetInternalSendID(string sendID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
BEGIN TRANSACTION

MERGE c101_mta_send WITH (HOLDLOCK) AS target
USING (SELECT @sndID) AS source(mta_send_id)
ON (target.mta_send_id = source.mta_send_id)
WHEN NOT MATCHED THEN
	INSERT (mta_send_id, mta_send_internalId, mta_send_createdTimestamp)
	VALUES (@sndID, ISNULL((SELECT MAX(mta_send_internalID) + 1 FROM c101_mta_send), 1, GETDATE()));

COMMIT TRANSACTION

SELECT mta_send_internalId
FROM c101_mta_send
WHERE mta_send_id = @sndID";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				conn.Open();
				return (int)cmd.ExecuteScalar();
			}
		}
	}
}
