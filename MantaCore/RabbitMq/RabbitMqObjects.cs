using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaMTA.Core.RabbitMq
{
	/// <summary>
	/// Represents an Inbound Email that is going to be queued for relaying, but has not yet been.
	/// </summary>
	public class RabbitMqInboundMessage
	{
		/// <summary>
		/// ID of the Message. Same as MessageID header in raw email.
		/// </summary>
		public Guid MessageID { get; set; }

		/// <summary>
		/// The VirtualMTA group that the message should be sent through.
		/// </summary>
		public int VirtualMTAGroupID { get; set; }

		/// <summary>
		/// The Manta internal ID of the Send that this Email is appart of.
		/// </summary>
		public int InternalSendID { get; set; }

		/// <summary>
		/// The Mail From to use in the SMTP conversation when sending the Email.
		/// </summary>
		public string MailFrom { get; set; }

		/// <summary>
		/// The RCPT TO to use in the SMTP conversation.
		/// </summary>
		public string[] RcptTo { get; set; }

		/// <summary>
		/// The raw Email itself.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Create a new RabbitMqInboundMessage instance.
		/// </summary>
		public RabbitMqInboundMessage() { }

		/// <summary>
		/// Create a new RabbitMqInboundMessage instance.
		/// </summary>
		public RabbitMqInboundMessage(Guid messageID, int virtualMtaGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			MessageID = messageID;
			VirtualMTAGroupID = virtualMtaGroupID;
			InternalSendID = internalSendID;
			MailFrom = mailFrom;
			RcptTo = rcptTo;
			Message = message;
		}
	}

	public class RabbitMqInboundMessageCollection : List<RabbitMqInboundMessage>
	{
		/// <summary>
		/// RabbitMQ delivery tag to use to ack this collection.
		/// </summary>
		public ulong DeliveryTag { get; set; }
	}

}
