
namespace Colony101.MTA.Library.Enums
{
	internal enum MessageDestination
	{
		/// <summary>
		/// Default value.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Message is for delivery on this server.
		/// </summary>
		Self = 1,
		/// <summary>
		/// Message should be relayed.
		/// </summary>
		Relay = 2
	}
}
