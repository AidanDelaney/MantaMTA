using System;
using System.Collections.Concurrent;

namespace MantaMTA.Core.SendID
{
	internal static class SendIDManager
	{
		/// <summary>
		/// Collection of SendIDs
		/// Overtime this will get big so should be cleared every hourish.
		/// </summary>
		private static ConcurrentDictionary<string, SendID> _SendIDs = new ConcurrentDictionary<string, SendID>();
		
		/// <summary>
		/// Timestamp of when _SendIDs was last cleared. 
		/// </summary>
		private static DateTime _SendIdsLastCleared = DateTime.UtcNow;

		/// <summary>
		/// Gets the internal send ID to be used for the specified sendID.
		/// If it doesn't exist it will be created in the database.
		/// </summary>
		/// <param name="sendId"></param>
		/// <returns></returns>
		public static int GetInternalSendId(string sendId)
		{
			// Don't want send IDs sitting in memory for to long so clear every so often.
			if (_SendIdsLastCleared.AddHours(1) < DateTime.UtcNow)
			{
				lock (_SendIDs)
				{
					_SendIDs.Clear();
					_SendIdsLastCleared = DateTime.UtcNow;
				}
			}

			SendID sndID;

			// Try to get the send id from the cached collection.
			if (!_SendIDs.TryGetValue(sendId, out sndID))
			{
				// Doesn't exist so need to create or load from datbase.
				sndID = new SendID()
				{
					ID = sendId,
					InternalID = DAL.SendIdDB.CreateAndGetInternalSendID(sendId)
				};

				// Add are new item to the cache.
				_SendIDs.TryAdd(sendId, sndID);
			}

			// return the value.
			return sndID.InternalID;
		}

		/// <summary>
		/// Gets the default send ID, based of the current time.
		/// </summary>
		/// <returns></returns>
		internal static int GetDefaultInternalSendId()
		{
			string sendID = DateTime.UtcNow.ToString("yyyyMMdd");
			return GetInternalSendId(sendID);
		}
	}
}
