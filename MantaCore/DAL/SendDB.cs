using System;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.Enums;
using MantaMTA.Core.Sends;

namespace MantaMTA.Core.DAL
{
	internal static class SendDB
	{
		/// <summary>
		/// Gets the sendID's internal ID from the database. If the record doesn't exist
		/// then it will be created.
		/// </summary>
		/// <param name="sendID">The SendID to get the internal ID for.</param>
		/// <returns></returns>
		public static Sends.Send CreateAndGetInternalSendID(string sendID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
BEGIN TRANSACTION

MERGE man_mta_send WITH (HOLDLOCK) AS target
USING (SELECT @sndID) AS source(mta_send_id)
ON (target.mta_send_id = source.mta_send_id)
WHEN NOT MATCHED THEN
	INSERT (mta_send_id, mta_sendStatus_id, mta_send_internalId, mta_send_createdTimestamp)
	VALUES (@sndID, @activeStatusID, ISNULL((SELECT MAX(mta_send_internalID) + 1 FROM man_mta_send), 1), GETUTCDATE());

COMMIT TRANSACTION

SELECT *
FROM man_mta_send
WHERE mta_send_id = @sndID";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				cmd.Parameters.AddWithValue("@activeStatusID", (int)SendStatus.Active);
				return DataRetrieval.GetSingleObjectFromDatabase<Sends.Send>(cmd, CreateAndFillSendFromRecord);
			}
		}

		/// <summary>
		/// Creates a SendID object and fills it with values from the datarecord.
		/// </summary>
		/// <param name="record">Record to get data from.</param>
		/// <returns>SendID filled with data.</returns>
		private static Sends.Send CreateAndFillSendFromRecord(IDataRecord record)
		{
			Sends.Send sendID = new Sends.Send();
			sendID.ID = record.GetString("mta_send_id");
			sendID.InternalID = record.GetInt32("mta_send_internalId");
			sendID.SendStatus = (SendStatus)record.GetInt32("mta_sendStatus_id");
			sendID.LastAccessedTimestamp = DateTime.UtcNow;

			return sendID;
		}

		/// <summary>
		/// Pause the specified send.
		/// </summary>
		/// <param name="internalSendID">Internal SendID of the send to pause.</param>
		public static void PauseSend(int internalSendID)
		{
			UpdateSendStatus(internalSendID, SendStatus.Paused);
		}

		/// <summary>
		/// Sets the specified sends status to discard.
		/// </summary>
		/// <param name="internalSendID">Internal SendID of the send.</param>
		public static void DiscardSend(int internalSendID)
		{
			UpdateSendStatus(internalSendID, SendStatus.Discard);
		}

		/// <summary>
		/// Sets the specified sends status to active.
		/// </summary>
		/// <param name="internalSendID">Internal SendID of the send.</param>
		public static void ResumeSend(int internalSendID)
		{
			UpdateSendStatus(internalSendID, SendStatus.Active);
		}

		/// <summary>
		/// Updates a sends send status.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send to pause.</param>
		/// <param name="sendStatus">SendStatus to update to.</param>
		private static void UpdateSendStatus(int internalSendID, SendStatus sendStatus)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
UPDATE man_mta_send
SET mta_sendStatus_id = @sendStatus
WHERE mta_send_internalId = @internalSndID";
				cmd.Parameters.AddWithValue("@sendStatus", (int)sendStatus);
				cmd.Parameters.AddWithValue("@internalSndID", (int)internalSendID);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="internalSendID"></param>
		/// <returns></returns>
		internal static Send GetSend(int internalSendID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT *
FROM man_mta_send
WHERE mta_send_internalId = @internalSndID";
				cmd.Parameters.AddWithValue("@internalSndID", internalSendID);
				return DataRetrieval.GetSingleObjectFromDatabase<Send>(cmd, CreateAndFillSendFromRecord);
			}
		}
	}
}
