using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// An Event from Manta indicating an issue sending an email.  Could be a bounce or an abuse complaint.
	/// </summary>
	public class MantaEvent
	{
		/// <summary>
		/// The type of event that this is.
		/// </summary>
		public MantaEventType EventType { get; set; }
		/// <summary>
		/// The email address that this message was sent to.
		/// </summary>
		public string EmailAddress { get; set; }
		/// <summary>
		/// The internal identifier for the send.
		/// </summary>
		public string SendID { get; set; }
		/// <summary>
		/// The date and time the event was recorded.
		/// </summary>
		public DateTime EventTime { get; set; }
	}

	/// <summary>
	/// Manta Bounce Event Notification.
	/// </summary>
	public class MantaBounceEvent : MantaEvent
	{
		/// <summary>
		/// The type of bounce.
		/// </summary>
		public MantaBounceType BounceType { get; set; }
		/// <summary>
		/// The code of the type of bounce.
		/// </summary>
		public MantaBounceCode BounceCode { get; set; }
		/// <summary>
		/// The text of the failure message. (Up to the number of characters configured.)
		/// </summary>
		public string Message { get; set; }
	}

	/// <summary>
	/// Manta Spam Complaint (Abuse) event.  The result of an email coming back from a feedback loop with an ISP.
	/// </summary>
	public class MantaAubseEvent : MantaEvent
	{
		
	}
}
