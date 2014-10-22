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
		/// ID of the Event.
		/// </summary>
		public int ID { get; set; }
		/// <summary>
		/// The type of event that this is.
		/// </summary>
		public MantaEventType EventType { get; set; }
		/// <summary>
		/// The email address that this message was sent to.
		/// </summary>
		public string EmailAddress { get; set; }
		/// <summary>
		/// The identifier for the send.
		/// </summary>
		public string SendID { get; set; }
		/// <summary>
		/// The date and time the event was recorded.
		/// </summary>
		public DateTime EventTime { get; set; }
		/// <summary>
		/// Will be set to true when event has been forwarded.
		/// </summary>
		public bool Forwarded { get; set; }

		public MantaEvent()
		{
			ID = 0;
			Forwarded = false;
		}
	}

	/// <summary>
	/// A collection of MantaEvents.
	/// </summary>
	public class MantaEventCollection : List<MantaEvent>
	{
		public MantaEventCollection() { }
		public MantaEventCollection(IEnumerable<MantaEvent> collection) : base(collection) { }
	}

	/// <summary>
	/// Manta Bounce Event Notification.
	/// </summary>
	public class MantaBounceEvent : MantaEvent
	{
		/// <summary>
		/// The type of bounce.
		/// </summary>
		public BouncePair BounceInfo;
		/// <summary>
		/// The text of the failure message. (Up to the number of characters configured.)
		/// </summary>
		public string Message { get; set; }
	}


	/// <summary>
	/// Manta Spam Complaint (Abuse) event.  The result of an email coming back from a feedback loop with an ISP.
	/// </summary>
	public class MantaAbuseEvent : MantaEvent
	{
		
	}


	/// <summary>
	/// Holds information about an SMTP code returned by a server as a bounce.
	/// </summary>
	public struct BouncePair
	{
		/// <summary>
		/// The MantaBounceCode for the Bounce.
		/// </summary>
		public MantaBounceCode BounceCode;
		/// <summary>
		/// The MentaBounceType for the Bounce.
		/// </summary>
		public MantaBounceType BounceType;

		public override string ToString()
		{
			return String.Format("BounceType: {0}, BounceCode: {1}", this.BounceType, this.BounceCode);
		}
	}
}
