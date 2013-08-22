
namespace MantaMTA.Core.Enums
{
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
