using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events
{
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
		Abuse = 3,
		/// <summary>
		/// Event occurs when a message has been in Manta's outbound queue over the <paramref name="MantaMTA.Core.MtaParameters.MtaMaxTimeInQueue"/> value
		/// without it being successfully accepted by a remote MTA. Manta stops attempting to send it and creates a TimedOutInQueue Event.
		/// </summary>
		TimedOutInQueue = 4
	}


	/// <summary>
	/// Identifies the type of bounce.
	/// </summary>
	public enum MantaBounceType : int
	{
		/// <summary>
		/// Default value.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// A hard bounce is where the ISP is explicitly saying that this email address is not valid. 
		/// Typically this is a "user does not exist" error message.
		/// </summary>
		Hard = 1,
		/// <summary>
		/// A soft bounce is an error condition, which if it continues, the email address should be considered 
		/// invalid. An example is a "DNS Failure" (MantaBounceCode 21) bounce message. This can happen because the domain 
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
		/// Default value.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Not actually a bounce.  Gives code processing a bounce a way of finding that it's not actually looking at a bounce, e.g. when finding a 
		/// </summary>
		NotABounce = 1,
		/// <summary>
		/// There is no email account for the email address specified.
		/// </summary>
		BadEmailAddress = 11,
		General = 20,
		DnsFailure = 21,
		MailboxFull = 22,
		MessageSizeTooLarge = 23,
		UnableToConnect = 29,
		ServiceUnavailable = 30,
		/// <summary>
		/// A bounce that we're unable to identify a reason for.
		/// </summary>
		BounceUnknown = 40,
		/// <summary>
		/// Sending server appears on a blocking list.
		/// </summary>
		KnownSpammer = 51,
		/// <summary>
		/// The content of the email has been identified as spam.
		/// </summary>
		SpamDetected = 52,
		AttachmentDetected = 53,
		RelayDenied = 54,
		/// <summary>
		/// Used when a receiving MTA has indicated we're sending too many emails to them.
		/// </summary>
		RateLimitedByReceivingMta = 55,
		/// <summary>
		/// Indicates the receiving server reported an error with the sending address provided by Manta.
		/// </summary>
		ConfigurationErrorWithSendingAddress = 56,
		/// <summary>
		/// The receiving MTA has blocked the IP address.  Contact them to have it removed.
		/// </summary>
		PermanentlyBlockedByReceivingMta = 57,
		/// <summary>
		/// The receiving MTA has placed a temporary block on the IP address, but will automatically remove it after a short period.
		/// </summary>
		TemporarilyBlockedByReceivingMta = 58
	}
}
