using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// 
	/// </summary>
	public enum EmailProcessingResult
	{
		/// <summary>
		/// Default value.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Email successfully processed - was abuse.
		/// </summary>
		SuccessAbuse = 1,
		/// <summary>
		/// Email successfully processed - was a bounce.
		/// </summary>
		SuccessBounce = 2,
		/// <summary>
		/// Error processing email - problem with content.
		/// </summary>
		ErrorContent = 3,
		/// <summary>
		/// Error processing email - file doesn't exist.
		/// </summary>
		ErrorNoFile = 4,
		/// <summary>
		/// Error processing email - no reason given.
		/// </summary>
		ErrorNoReason = 5,
		/// <summary>
		/// No return path was found as such we identify
		/// email address or send.
		/// </summary>
		ErrorNoReturnPath = 6
	}


	/// <summary>
	/// This identifies the type of an event.
	/// </summary>
	public enum MantaEventType : int
	{
		/// <summary>
		/// Event of a Type unknown
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Event occurs when delivery of a message is unsuccessful.
		/// </summary>
		Bounce = 1,
		/// <summary>
		/// This event occurs so that you can remove the address that generated the complaint from your database or 
		/// stop sending to it. You can also use this event to maintain statistical information on the number of spam 
		/// complaints created by each campaign. Continuing to send to an address that has complained about spam can 
		/// have a negative effect on your deliverability.
		/// </summary>
		Abuse = 3
	}


	/// <summary>
	/// Identifies the type of bounce.
	/// </summary>
	public enum MantaBounceType : int
	{
		/// <summary>
		/// A hard bounce is where the ISP is explicitly saying that this email address is not valid. 
		/// Typically this is a "user does not exist" error message.
		/// </summary>
		Hard = 1,
		/// <summary>
		/// A soft bounce is an error condition, which if it continues, the email address should be considered 
		/// invalid. An example is a "DNS Failure" (code 21) bounce message. This can happen because the domain 
		/// name no longer exists, or this could happen because the DNS registration expired and will be renewed
		/// tomorrow, or there was a temporary DNS lookup error. If the "DNS failure" messages persist, then we 
		/// know the address is bad.
		/// </summary>
		Soft = 2,
		/// <summary>
		/// The email was rejected as spam. Instead of removing the email addresses from your list, you would
		/// want to solve whatever caused the blocking and restore delivery to these addresses.
		/// </summary>
		Spam = 3
	}


	/// <summary>
	/// Manta notification bounce code.
	/// These identify the type of bounce that has occurred.
	/// </summary>
	public enum MantaBounceCode : int
	{
		/// <summary>
		/// 550 User Unknown
		/// </summary>
		RejectedBadEmailAddress = 10,
		DeferredGeneral = 20,
		DeferredDNSFailure = 21,
		DeferredMailboxFull = 22,
		DeferredMessageSizeTooLarge = 23,
		DeferredUnableToConnect = 29,
		/// <summary>
		/// A bounce that we're unable to identify a reason for.
		/// </summary>
		BounceUnknown = 40,
		RejectedUnknown = 50,
		/// <summary>
		/// 
		/// </summary>
		RejectedKnownSpammer = 51,
		/// <summary>
		/// 
		/// </summary>
		RejectedSpamDetected = 52,
		RejectedAttachmentDetected = 53,
		RejectedRelayDenied = 54,
		RejectedUnableToConnect = 59
	}
}
