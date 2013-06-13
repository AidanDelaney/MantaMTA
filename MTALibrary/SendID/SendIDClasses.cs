using System;

namespace Colony101.MTA.Library.SendID
{
	internal class SendID
	{
		/// <summary>
		/// The textual send ID.
		/// </summary>
		public string ID { get; set; }
		/// <summary>
		/// An Internal ID for the sendID.
		/// </summary>
		public int InternalID { get; set; }
		/// <summary>
		/// This is used to record when this instance of this class was accessed. Used by
		/// the SendIDManager to clean up it's internal cache.
		/// </summary>
		public DateTime LastAccessedTimestamp { get; set; }
	}
}
