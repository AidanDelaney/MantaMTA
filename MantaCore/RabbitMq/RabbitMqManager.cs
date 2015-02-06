using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;

namespace MantaMTA.Core.RabbitMq
{
	public static class RabbitMqManager
	{
		/// <summary>
		/// The connection to the RabbitMQ instance.
		/// </summary>
		private static readonly IConnection LocalhostConnection = new ConnectionFactory
		{
			HostName = "localhost",
			UserName = "mantamta",
			Password = "mantamta"
		}.CreateConnection();

		/// <summary>
		/// Used to format Objects into JSON for queuing in RabbitMQ.
		/// </summary>
		private static JavaScriptSerializer JsonFormatter = new JavaScriptSerializer();

		/// <summary>
		/// (Spec method) Acknowledge one or more delivered message(s).
		/// </summary>
		/// <param name="queue">Queue the message or messages are from.</param>
		/// <param name="deliveryTag">ID of the message delivery.</param>
		/// <param name="multiple">Ack all deliverys upto and including specified.</param>
		public static void Ack(RabbitMqQueue queue, ulong deliveryTag, bool multiple)
		{
			using (IModel channel = GetChannel(queue))
			{
				var consumer = new QueueingBasicConsumer(channel);
				channel.BasicConsume(GetQueueNameFromEnum(queue), false, consumer);
				channel.BasicAck(deliveryTag, multiple);
			}
		}

		/// <summary>
		/// Dequeues the specified amount of messages from the queue. If there are less messages queued than request will give less.
		/// </summary>
		/// <param name="queue">The queue to dequeue from.</param>
		/// <param name="maxItems">The maximum amount of messages to dequeue.</param>
		/// <param name="millisecondsTimeout">If queue is empty the max time to wait for more to appear.</param>
		/// <returns>List of BasicDeliverEventArgs.</returns>
		public static List<BasicDeliverEventArgs> Dequeue(RabbitMqQueue queue, int maxItems, int millisecondsTimeout)
		{
			using (IModel channel = GetChannel(queue))
			{
				List<BasicDeliverEventArgs> items = new List<BasicDeliverEventArgs>();
				QueueingBasicConsumer consumer = new QueueingBasicConsumer(channel);
				channel.BasicConsume(GetQueueNameFromEnum(queue), false, consumer);
				while (items.Count < maxItems)
				{
					BasicDeliverEventArgs ea = null;
					if (!consumer.Queue.Dequeue(millisecondsTimeout, out ea))
						break;

					items.Add(ea);
				}

				return items;
			}
		}

		/// <summary>
		/// Gets the Common AMQP model for the specified queue, using the the specified connection.
		/// </summary>
		/// <param name="queue">The queue to get the AMQP model for.</param>
		/// <returns>Common AMQP model.</returns>
		private static IModel GetChannel(RabbitMqQueue queue)
		{
			IModel channel = LocalhostConnection.CreateModel();
			channel.QueueDeclare(	GetQueueNameFromEnum(queue),	// The Queue to use.
									true,							// True as we want the queue durable.
									false,							// Queue isn't exclusive.
									false,							// Don't auto delete from the queue.
									null);							// No additional args required.
			return channel;
		}

		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static void Publish(byte[] message, RabbitMqQueue queue)
		{
			using (IModel channel = GetChannel(queue))
			{
				IBasicProperties msgProps = channel.CreateBasicProperties();
				msgProps.SetPersistent(true);
				channel.BasicPublish(string.Empty, GetQueueNameFromEnum(queue), msgProps, message);
			}
		}

		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static void Publish(string message, RabbitMqQueue queue)
		{
			byte[] body = Encoding.UTF8.GetBytes(message);
			Publish(body, queue);
		}

		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static void Publish(object obj, RabbitMqQueue queue)
		{
			string str = JsonFormatter.Serialize(obj);
			Publish(str, queue);
		}

		/// <summary>
		/// Gets the RabbitMQ queue name for the specified queue.
		/// </summary>
		/// <param name="queue">Queue to get the name of.</param>
		/// <returns>The name of the queue.</returns>
		private static string GetQueueNameFromEnum(RabbitMqQueue queue)
		{
			switch (queue)
			{
				case RabbitMqQueue.Inbound:
					return "manta_mta_inbound";
				default:
					throw new Exception("Cannot get name for RabbitMqQueue");
			}
		}

		/// <summary>
		/// Specifies the Queue in RabbitMQ.
		/// </summary>
		public enum RabbitMqQueue
		{
			/// <summary>
			/// The Inbound Queue is a queue of messages that have been received and will be relayed.
			/// </summary>
			Inbound = 0
		}
	}
}
