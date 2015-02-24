using MantaMTA.Core.Client.BO;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace MantaMTA.Core.RabbitMq
{
	internal static class RabbitMqInboundQueueManager
	{
		private static JavaScriptSerializer JsonFormatter = new JavaScriptSerializer();

		/// <summary>
		/// Dequeues a collection of inbound messages from RabbitMQ.
		/// </summary>
		/// <param name="maxItems">The maximum amount of messages to dequeue.</param>
		/// <returns>The dequeue messages.</returns>
		public static MtaMessageCollection Dequeue(int maxItems)
		{
			List<BasicDeliverEventArgs> items = RabbitMqManager.Dequeue(RabbitMqManager.RabbitMqQueue.Inbound, maxItems, 1 * 1000);
			MtaMessageCollection messages = new MtaMessageCollection();
			if (items.Count == 0)
				return messages;

			foreach (BasicDeliverEventArgs ea in items)
			{
				string json = Encoding.UTF8.GetString(ea.Body);
				MtaMessage msg = JsonFormatter.Deserialize<MtaMessage>(json);
				msg.RabbitMqDeliveryTag = ea.DeliveryTag;
				messages.Add(msg);
			}

			return messages;
		}

		/// <summary>
		/// Enqueues the Email that we are going to relay in RabbitMQ.
		/// </summary>
		/// <param name="messageID">ID of the Message being Queued.</param>
		/// <param name="ipGroupID">ID of the Virtual MTA Group to send the Message through.</param>
		/// <param name="internalSendID">ID of the Send the Message is apart of.</param>
		/// <param name="mailFrom">The envelope mailfrom, should be return-path in most instances.</param>
		/// <param name="rcptTo">The envelope rcpt to.</param>
		/// <param name="message">The Email.</param>
		/// <returns>True if the Email has been enqueued in RabbitMQ.</returns>
		public static bool Enqueue(Guid messageID, int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			// Create the thing we are going to queue in RabbitMQ.
			MtaMessage recordToSave = new MtaMessage(messageID,
				ipGroupID,
				internalSendID,
				mailFrom,
				rcptTo,
				message);

			RabbitMqManager.Publish(recordToSave, RabbitMqManager.RabbitMqQueue.Inbound);
			return true;
		}
	}
}
