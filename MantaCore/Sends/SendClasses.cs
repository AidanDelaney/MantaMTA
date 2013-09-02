using System;
using MantaMTA.Core.Enums;

namespace MantaMTA.Core.Sends
{
	public class Send
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
		/// The current Status of this Send.
		/// </summary>
		public SendStatus SendStatus { get; set; }
		/// <summary>
		/// This is used to record when this instance of this class was accessed. Used by
		/// the SendIDManager to clean up it's internal cache.
		/// </summary>
		public DateTime LastAccessedTimestamp { get; set; }
		/// <summary>
		/// 
		/// </summary>
		public DateTime CreatedTimestamp { get; set; }
	}
}
