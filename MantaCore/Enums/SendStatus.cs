
namespace MantaMTA.Core.Enums
{
	public enum SendStatus : int
	{
		/// <summary>
		/// The send is active; Emails can be sent.
		/// </summary>
		Active = 1,
		/// <summary>
		/// The send is paused; Emails shouldn't be sent yet.
		/// </summary>
		Paused = 2,
		/// <summary>
		/// The send is cancelled; Emails should never be sent.
		/// </summary>
		Discard = 3
	}
}
