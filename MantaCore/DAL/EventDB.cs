using System;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.Events;

namespace MantaMTA.Core.DAL
{
	internal static class EventDB
	{
		/// <summary>
		/// Gets a MantaEvent from the database.
		/// </summary>
		/// <returns>The event from the database of NULL if one wasn't found with the ID</returns>
		public static MantaEvent GetEvent(int ID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT [evt].*, [bnc].evn_bounceCode_id, [bnc].evn_bounceEvent_message, [bnc].evn_bounceType_id
FROM man_evn_event AS [evt]
LEFT JOIN man_evn_bounceEvent AS [bnc] ON [evt].evn_event_id = [bnc].evn_event_id
WHERE [evt].evn_event_id = @eventId";
				cmd.Parameters.AddWithValue("@eventId", ID);
				return DataRetrieval.GetSingleObjectFromDatabase<MantaEvent>(cmd, CreateAndFillMantaEventFromRecord);
			}
		}

		/// <summary>
		/// Gets all of the MantaEvents from the database.
		/// </summary>
		/// <returns>Collection of MantaEvent objects.</returns>
		public static MantaEventCollection GetEvents()
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"SELECT [evt].*, [bnc].evn_bounceCode_id, [bnc].evn_bounceEvent_message, [bnc].evn_bounceType_id
FROM man_evn_event AS [evt]
LEFT JOIN man_evn_bounceEvent AS [bnc] ON [evt].evn_event_id = [bnc].evn_event_id";
				return new MantaEventCollection(DataRetrieval.GetCollectionFromDatabase<MantaEvent>(cmd, CreateAndFillMantaEventFromRecord));
			}
		}

		/// <summary>
		/// Gets <param name="maxEventsToGet"/> amount of Events that need forwarding from the database.
		/// </summary>
		public static MantaEventCollection GetEventsForForwarding(int maxEventsToGet)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
SELECT TOP " + maxEventsToGet + @" [evt].*, [bnc].evn_bounceCode_id, [bnc].evn_bounceEvent_message, [bnc].evn_bounceType_id
FROM man_evn_event AS [evt]
LEFT JOIN man_evn_bounceEvent AS [bnc] ON [evt].evn_event_id = [bnc].evn_event_id
WHERE evn_event_forwarded = 0
ORDER BY evn_event_id ASC";
				return new MantaEventCollection(DataRetrieval.GetCollectionFromDatabase<MantaEvent>(cmd, CreateAndFillMantaEventFromRecord));
			}
		}

		/// <summary>
		/// Saves the MantaEvent to the database.
		/// </summary>
		/// <param name="evn">The Manta Event to save.</param>
		/// <returns>ID of the MantaEvent.</returns>
		public static int Save(MantaEvent evn)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM man_evn_event WHERE evn_event_id = @eventID)
	BEGIN
		UPDATE man_evn_event
		SET evn_type_id = @eventType,
		evn_event_timestamp = @timestamp,
		evn_event_emailAddress = @emailAddress,
		snd_send_id = @sendId,
		evn_event_forwarded = @forwarded
		WHERE evn_event_id = @eventID
	END
ELSE
	BEGIN
		INSERT INTO man_evn_event(evn_type_id, evn_event_timestamp, evn_event_emailAddress, snd_send_id, evn_event_forwarded)
		VALUES(@eventType, @timestamp, @emailAddress, @sendId, @forwarded)

		SET @eventID = @@IDENTITY
	END
";
				cmd.Parameters.AddWithValue("@eventID", evn.ID);
				cmd.Parameters.AddWithValue("@eventType", (int)evn.EventType);
				cmd.Parameters.AddWithValue("@timestamp", evn.EventTime);
				cmd.Parameters.AddWithValue("@emailAddress", evn.EmailAddress);
				cmd.Parameters.AddWithValue("@sendId", evn.SendID);
				cmd.Parameters.AddWithValue("@forwarded", evn.Forwarded);

				if (evn is MantaBounceEvent)
				{
					cmd.CommandText += @"
IF EXISTS (SELECT 1 FROM man_evn_bounceEvent WHERE evn_event_id = @eventId)
		UPDATE man_evn_bounceEvent
		SET evn_bounceCode_id = @bounceCode,
		evn_bounceEvent_message = @message,
		evn_bounceType_id = @bounceType
		WHERE evn_event_id = @eventId
ELSE
		INSERT INTO man_evn_bounceEvent(evn_event_id, evn_bounceCode_id, evn_bounceEvent_message, evn_bounceType_id)
		VALUES(@eventId, @bounceCode, @message, @bounceType)
";

					cmd.Parameters.AddWithValue("@bounceCode", (int)(evn as MantaBounceEvent).BounceInfo.BounceCode);
					cmd.Parameters.AddWithValue("@message", (evn as MantaBounceEvent).Message);
					cmd.Parameters.AddWithValue("@bounceType", (int)(evn as MantaBounceEvent).BounceInfo.BounceType);
				}

				cmd.CommandText += @"SELECT @eventID
";

				conn.Open();
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
		}

		/// <summary>
		/// Creates a MantaEvent object and Fills it with the values from the data record.
		/// </summary>
		/// <param name="record">Record to get the data from.</param>
		/// <returns>MantaAubseEvent or MantaBounceEvent</returns>
		private static MantaEvent CreateAndFillMantaEventFromRecord(IDataRecord record)
		{
			MantaEventType type = (MantaEventType)record.GetInt32("evn_type_id");
			MantaEvent thisEvent = null;
			switch (type)
			{
				case MantaEventType.Abuse:
					thisEvent = new MantaAbuseEvent();
					break;
				case MantaEventType.Bounce:
					thisEvent = new MantaBounceEvent();
					FillMantaBounceEvent((thisEvent as MantaBounceEvent), record);
					break;
				default:
					throw new NotImplementedException("Unknown Event Type (" + type + ")");
			}

			thisEvent.EmailAddress = record.GetString("evn_event_emailAddress");
			thisEvent.EventTime = record.GetDateTime("evn_event_timestamp");
			thisEvent.EventType = type;
			thisEvent.ID = record.GetInt32("evn_event_id");
			thisEvent.SendID = record.GetString("snd_send_id");
			thisEvent.Forwarded = record.GetBoolean("evn_event_forwarded");
			return thisEvent;
		}

		/// <summary>
		/// Fills the MantaBounceEvent with values from <paramref name="record"/>
		/// </summary>
		/// <param name="evt">The MantaBounceEvent to fill.</param>
		/// <param name="record">The data record to fill with.</param>
		private static void FillMantaBounceEvent(MantaBounceEvent evt, IDataRecord record)
		{
			evt.BounceInfo = new BouncePair
			{
				BounceCode = (MantaBounceCode)record.GetInt32("evn_bounceCode_id"),
				BounceType = (MantaBounceType)record.GetInt32("evn_bounceType_id")
			};
			evt.Message = record.GetString("evn_bounceEvent_message");
		}
	}
}
