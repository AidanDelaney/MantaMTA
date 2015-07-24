using System;
using System.Collections.Generic;

namespace MantaMTA.Core.Client.BO
{
    /// <summary>
    /// Represents an Inbound Email that is going to be queued for relaying, but has not yet been.
    /// </summary>
    public class MtaMessage
	{
		/// <summary>
		/// ID of the RabbitMQ delivery that this message represents.
		/// </summary>
		public ulong RabbitMqDeliveryTag { get; set; }

		/// <summary>
		/// Unique identifier for this message.
		/// </summary>
		public Guid ID { get; set; }

		/// <summary>
		/// The VirtualMTA group that the message should be sent through.
		/// </summary>
		public int VirtualMTAGroupID { get; set; }

		/// <summary>
		/// Internal ID that identifies the Send that this 
		/// message is part of.
		/// </summary>
		public int InternalSendID { get; set; }

		/// <summary>
		/// The Mail From to used when sending this message.
		/// May be NULL for NullSender
		/// </summary>
		public string MailFrom { get; set; }

		/// <summary>
		/// Array of Rcpt To's for this message.
		/// </summary>
		public string[] RcptTo { get; set; }

		/// <summary>
		/// The raw Email itself.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Create a new RabbitMqInboundMessage instance.
		/// </summary>
		public MtaMessage()
		{
			RabbitMqDeliveryTag = 0;
		}

		/// <summary>
		/// Create a new RabbitMqInboundMessage instance.
		/// </summary>
		public MtaMessage(Guid messageID, int virtualMtaGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			ID = messageID;
			VirtualMTAGroupID = virtualMtaGroupID;
			InternalSendID = internalSendID;
			MailFrom = mailFrom;
			RcptTo = rcptTo;
			Message = message;
			RabbitMqDeliveryTag = 0;
		}
	}

	/// <summary>
	/// Holds a collection of MtaMessages.
	/// </summary>
	internal class MtaMessageCollection : List<MtaMessage>
	{
		public MtaMessageCollection(IEnumerable<MtaMessage> collection) : base(collection) { }
		public MtaMessageCollection() { }
	}
}
