using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Linq;

namespace MantaMTA.Core.DAL
{
	internal class RedisDB
	{
		/// <summary>
		/// This is the connection to Redis.
		/// </summary>
		private static ConnectionMultiplexer _RedisConnection = null;

		/// <summary>
		/// This is the prefix for inbound emails in Redis.
		/// </summary>
		private const string REDIS_EMAIL_KEY_PREFIX = "eml_";

		/// <summary>
		/// The maximum amount of inbound messages to bulk insert into SQL Server in a single transaction.
		/// </summary>
		private const int REDIS_TO_SQL_MAX_TRANSACTION_RECORDS = 50;

		/// <summary>
		/// Used to format Emails into JSON for storage in Redis.
		/// </summary>
		private static JavaScriptSerializer JsonFormatter = new JavaScriptSerializer();

		/// <summary>
		/// Lock used when attepting to connect to Redis.
		/// </summary>
		private static object _RedisConnectionLock = new object();

		/// <summary>
		/// Timestamp of the last failed attempt to connect to Redis.
		/// </summary>
		private static DateTime _LastConnectAttempt = DateTime.MinValue;

		/// <summary>
		/// Gets a Redis Database.
		/// </summary>
		/// <param name="db">ID of the database to get.</param>
		/// <returns>The Redis Database.</returns>
		private static IDatabase GetRedisDatabase(int db = 0)
		{
			lock (_RedisConnectionLock)
			{
				// If there is no Redis connection or it isn't connected then we need to attempt to connect.
				if (_RedisConnection == null ||
					_RedisConnection.IsConnected == false)
				{
					try
					{
						// If attempts to connect are failing only attempt to connect once a minute so that sending can continue
						// by going straight to SQL server.
						if (_LastConnectAttempt < DateTime.UtcNow.AddMinutes(-1))
						{
							_RedisConnection = ConnectionMultiplexer.Connect("localhost,connectTimeout=1000");
						}
					}
					catch (TimeoutException)
					{
						// Connect failed. Most likely Redis isn't running.
						Logging.Error("Redis connect() timeout; Ensure Redis is running, will not attept to reconnect for 1 minute.");
						_LastConnectAttempt = DateTime.UtcNow;
					}
				}
			}

			// If we have failed to create a connect to Redis then we cannot get the database so return null.
			if (_RedisConnection == null || !_RedisConnection.IsConnected)
				return null;

			// Get the database.
			return _RedisConnection.GetDatabase(db);
		}

		/// <summary>
		/// Enqueues the Inbound Message in Redis.
		/// </summary>
		/// <param name="messageID">ID of the Message being Queued.</param>
		/// <param name="ipGroupID">ID of the Virtual MTA Group to send the Message through.</param>
		/// <param name="internalSendID">ID of the Send the Message is apart of.</param>
		/// <param name="mailFrom">The envelope mailfrom, should be return-path in most instances.</param>
		/// <param name="rcptTo">The envelope rcpt to.</param>
		/// <param name="message">The Email.</param>
		/// <returns>True if the Message has been queued in Redis, false if not.</returns>
		internal static bool EnqueueMessage(Guid messageID, int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			// Get the Database.
			IDatabase db = GetRedisDatabase();

			// If no database then we can't save.
			if (db == null)
				return false;

			// Build a unique key for this message.
			string key = REDIS_EMAIL_KEY_PREFIX + messageID.ToString("N");

			// Create the thing we are going to store in Redis.
			RedisMessage recordToSave = new RedisMessage(messageID,
				ipGroupID,
				internalSendID,
				mailFrom,
				rcptTo,
				message);

			// Convert object to JSON and save to Redis.
			return db.StringSet(key, JsonFormatter.Serialize(recordToSave));
		}

		/// <summary>
		/// Gets a collection of Messages from Redis.
		/// </summary>
		/// <returns>A collection of Messages from Redis or NULL if there aren't any.</returns>
		internal static RedisMessageCollection GetQueuedMessages()
		{
			// Get the Database.
			IDatabase db = GetRedisDatabase();

			// If no database then we can't get any messages.
			if (db == null)
				return null;

			// Get the Redis Server.
			IServer s = _RedisConnection.GetServer(_RedisConnection.GetEndPoints()[0]);
			// Get a list of the Inbound Email keys from the Server.
			List<RedisKey> foundKeys = s.Keys(pattern: REDIS_EMAIL_KEY_PREFIX + "*").ToList();

			// Workout how many Emails we are going to get and return.
			int transactionsToGet = (foundKeys.Count < REDIS_TO_SQL_MAX_TRANSACTION_RECORDS ? foundKeys.Count : REDIS_TO_SQL_MAX_TRANSACTION_RECORDS);

			// Collection of results to return.
			RedisMessageCollection results = new RedisMessageCollection();

			// Get the Emails from Redis.
			for (int i = 0; i < transactionsToGet; i++)
			{
				RedisKey key = foundKeys[i];
				string value = db.StringGet(key);
				results.Add(JsonFormatter.Deserialize<RedisMessage>(value));
			}

			return results;
		}

		/// <summary>
		/// Deletes the specified Message from Redis.
		/// </summary>
		/// <param name="msg">The Message to delete.</param>
		/// <returns>True if deleted, false if not.</returns>
		internal static bool DeleteMessage(RedisMessage msg)
		{
			// Get the Database.
			IDatabase db = GetRedisDatabase();

			// If no database then can't delete.
			if (db == null)
				return false;

			// Do the Delete.
			return db.KeyDelete(REDIS_EMAIL_KEY_PREFIX + msg.MessageID.ToString("N"));
		}

		/// <summary>
		/// Represents a Queued Email that is stored in Redis.
		/// </summary>
		public class RedisMessage
		{
			public Guid MessageID { get; set; }
			public int VirtualMTAGroupID { get; set; }
			public int InternalSendID { get; set; }
			public string MailFrom { get; set; }
			public string[] RcptTo { get; set; }
			public string Message { get; set; }

			public RedisMessage() { }

			public RedisMessage(Guid messageID, int virtualMtaGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
			{
				MessageID = messageID;
				VirtualMTAGroupID = virtualMtaGroupID;
				InternalSendID = internalSendID;
				MailFrom = mailFrom;
				RcptTo = rcptTo;
				Message = message;
			}
		}

		/// <summary>
		/// Collection of Redis Queued Emails.
		/// </summary>
		public class RedisMessageCollection : List<RedisMessage> { }
	}
}
