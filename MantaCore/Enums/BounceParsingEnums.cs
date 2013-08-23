
namespace MantaMTA.Core.Enums
{
	/// <summary>
	/// Indicates the result of processing an email, e.g. it was a bounce and we could identify what type of bounce, or it was a Feedback Abuse email.
	/// Also includes some error situations.
	/// </summary>
	public enum EmailProcessingResult
	{
		/// <summary>
		/// Used by the EmailProcessingDetails class to indicate when processing of an email hasn't gone far enough for a suitable EmailProcessingResult value to have been set.
		/// Some methods use EmailProcessingDetails objects as parameters, but don't set the .ProcessingResult property of them as they're too low level for it to mean anything at the point they run.
		/// </summary>
		NotYetSet = -1,
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
		/// Email successfully processed - wasn't an abuse or bounce email; likely to be junk.
		/// </summary>
		SuccessNoAction = 3,
		/// <summary>
		/// Error processing email - problem with content.
		/// </summary>
		ErrorContent = 4,
		/// <summary>
		/// Error processing email - file doesn't exist.
		/// </summary>
		ErrorNoFile = 5,
		/// <summary>
		/// Error processing email - no reason given.
		/// </summary>
		ErrorNoReason = 6,
		/// <summary>
		/// No return path was found as such we identify
		/// email address or send.
		/// </summary>
		ErrorNoReturnPath = 7
	}


	/// <summary>
	/// How Manta identified an email as being a bounce, e.g. did a Bounce Rule match its content or was an NDR code found?
	/// </summary>
	public enum BounceIdentifier
	{
		/// <summary>
		/// Wasn't a bounce.
		/// </summary>
		NotIdentifiedAsABounce = 0,
		/// <summary>
		/// No Manta ReturnPath was found.
		/// </summary>
		UnknownReturnPath,
		/// <summary>
		/// A Bounce Rule matched the content.
		/// </summary>
		BounceRule,
		/// <summary>
		/// A Non-Delivery Report code was found.
		/// </summary>
		NdrCode,
		/// <summary>
		/// An SMTP code was found.
		/// </summary>
		SmtpCode
	}
}
