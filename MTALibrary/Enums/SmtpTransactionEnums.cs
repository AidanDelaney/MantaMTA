
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

	/// <summary>
	/// 
	/// </summary>
	internal enum TransactionStatus : int
	{
		/// <summary>
		/// 
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// Message delivery attempted failed.
		/// </summary>
		Deferred = 1,
		/// <summary>
		/// Message has failed and cannot be delivered.
		/// </summary>
		Failed = 2,
		/// <summary>
		/// Message was in the queue for to long and has timed out.
		/// </summary>
		TimedOut = 3,
		/// <summary>
		/// Message was successfully devlivered.
		/// </summary>
		Success = 4
	}
}
