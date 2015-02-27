using MantaMTA.Core.Client.BO;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MantaMTA.Core.RabbitMq
{
	internal static class RabbitMqOutboundQueueManager
	{
		/// <summary>
		/// Enqueue the messages in the collection for relaying.
		/// </summary>
		/// <param name="inboundMessages">Messages to enqueue.</param>
		public static void Enqueue(MtaMessageCollection inboundMessages)
		{
			Parallel.ForEach(inboundMessages, message=>{
				Enqueue(MtaQueuedMessage.CreateNew(message));
			});

			RabbitMqManager.Ack(RabbitMqManager.RabbitMqQueue.Inbound, inboundMessages.Max(m => m.RabbitMqDeliveryTag), true);
		}

		/// <summary>
		/// Enqueue the message for relaying.
		/// </summary>
		/// <param name="msg">Message to enqueue.</param>
		public static void Enqueue(MtaQueuedMessage msg)
		{
			RabbitMqManager.RabbitMqQueue queue = RabbitMqManager.RabbitMqQueue.OutboundWaiting;

			int secondsUntilNextAttempt = (int)Math.Ceiling((msg.AttemptSendAfterUtc - DateTime.UtcNow).TotalSeconds);

			if (secondsUntilNextAttempt > 0)
			{
				if (secondsUntilNextAttempt < 10)
					queue = RabbitMqManager.RabbitMqQueue.OutboundWait1;
				else if (secondsUntilNextAttempt < 60)
					queue = RabbitMqManager.RabbitMqQueue.OutboundWait10;
				else if (secondsUntilNextAttempt < 300)
					queue = RabbitMqManager.RabbitMqQueue.OutboundWait60;
				else
					queue = RabbitMqManager.RabbitMqQueue.OutboundWait300;
			}

			RabbitMqManager.Publish(msg, queue);
		}

		/// <summary>
		/// Dequeue a message from RabbitMQ.
		/// </summary>
		/// <returns>A dequeued message or null if there weren't any.</returns>
		public static MtaQueuedMessage Dequeue()
		{
			BasicDeliverEventArgs ea = RabbitMqManager.Dequeue(RabbitMqManager.RabbitMqQueue.OutboundWaiting, 1, 100).FirstOrDefault();
			if (ea == null)
				return null;

			MtaQueuedMessage qmsg = Serialisation.Deserialise<MtaQueuedMessage>(ea.Body);
			qmsg.RabbitMqDeliveryTag = ea.DeliveryTag;
			return qmsg;
		}

		/// <summary>
		/// Acknowledge the message as handled.
		/// </summary>
		/// <param name="msg">The message to acknowledge.</param>
		internal static void Ack(MtaQueuedMessage msg)
		{
			RabbitMqManager.Ack(RabbitMqManager.RabbitMqQueue.OutboundWaiting, msg.RabbitMqDeliveryTag, false);
		}
	}
}
