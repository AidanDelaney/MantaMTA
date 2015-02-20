using System.Collections.Generic;

namespace WebInterface.Models
{
	/// <summary>
	/// Holds information about a RabbitMQ queue.
	/// </summary>
	public class RabbitMqQueue
	{
		/// <summary>
		/// Name of the Queue.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Messages in the queue.
		/// </summary>
		public long Messages { get; set;}
		/// <summary>
		/// State of the queue.
		/// </summary>
		public string State { get; set; }
	}

	public class RabbitMqQueueModel : List<RabbitMqQueue>
    {
        
    }
}
