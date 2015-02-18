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
			HostName = MtaParameters.RabbitMQ.Hostname,
			UserName = MtaParameters.RabbitMQ.Username,
			Password = MtaParameters.RabbitMQ.Password
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
			IModel channel = GetChannel(queue);
			channel.BasicAck(deliveryTag, multiple);
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
			IModel channel = GetChannel(queue);
			List<BasicDeliverEventArgs> items = new List<BasicDeliverEventArgs>();
			QueueingBasicConsumer consumer = GetQueueingBasicConsumer(queue);
				
			while (items.Count < maxItems)
			{
				BasicDeliverEventArgs ea = null;
				if (!consumer.Queue.Dequeue(millisecondsTimeout, out ea))
					break;

				items.Add(ea);
			}

			return items;
		}

		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static void Publish(byte[] message, RabbitMqQueue queue)
		{
			IModel channel = GetChannel(queue);
			IBasicProperties msgProps = channel.CreateBasicProperties();
			msgProps.SetPersistent(true);
			channel.BasicPublish(string.Empty, GetQueueNameFromEnum(queue), msgProps, message);
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
				case RabbitMqQueue.OutboundWaiting:
					return "manta_mta_outbound_waiting";
				default:
					throw new Exception("Cannot get name for RabbitMqQueue");
			}
		}

		/// <summary>
		/// Holds the Channels to the RabbitMQ queues.
		/// </summary>
		private static Dictionary<RabbitMqQueue, IModel> _Channels = new Dictionary<RabbitMqQueue, IModel>();

		/// <summary>
		/// Lock for getting the channels, ensures only one exists per queue.
		/// </summary>
		private static object _GetChannelLock = new object();

		/// <summary>
		/// Gets the Common AMQP model for the specified queue, using the the specified connection.
		/// </summary>
		/// <param name="queue">The queue to get the AMQP model for.</param>
		/// <returns>Common AMQP model.</returns>
		private static IModel GetChannel(RabbitMqQueue queue)
		{
			lock (_GetChannelLock)
			{
				if (!_Channels.ContainsKey(queue))
					_Channels[queue] = null;

				IModel channel = _Channels[queue];
				
				// If the channel to the specified queue doesn't exist then we need to create it.
				if (channel == null)
				{
					channel = LocalhostConnection.CreateModel();
					channel.QueueDeclare(GetQueueNameFromEnum(queue),		// The Queue to use.
											true,							// True as we want the queue durable.
											false,							// Queue isn't exclusive.
											false,							// Don't auto delete from the queue.
											null);							// No additional args required.
				}

				_Channels[queue] = channel;
				return channel;
			}
		}

		/// <summary>
		/// Holds the RabbitMQ queue consumers.
		/// </summary>
		private static Dictionary<RabbitMqQueue, QueueingBasicConsumer> _Consumers = new Dictionary<RabbitMqQueue, QueueingBasicConsumer>();

		/// <summary>
		/// Lock for getting the consumers, ensures only one exists per queue.
		/// </summary>
		private static object _GetConsumerLock = new object();

		/// <summary>
		/// Gets the consumer for the specified queue.
		/// </summary>
		/// <param name="queue">The queue to get the consumer for.</param>
		/// <returns>The consumer for th specified queue.</returns>
		private static QueueingBasicConsumer GetQueueingBasicConsumer(RabbitMqQueue queue)
		{
			lock (_GetConsumerLock)
			{
				if (!_Consumers.ContainsKey(queue))
					_Consumers[queue] = null;

				QueueingBasicConsumer consumer = _Consumers[queue];

				// If the consumer doesn't exist we need to create it.
				if (consumer == null)
				{
					IModel channel = GetChannel(queue);
					consumer = new QueueingBasicConsumer(channel);
					channel.BasicConsume(GetQueueNameFromEnum(queue), false, consumer);
				}

				_Consumers[queue] = consumer;
				return consumer;
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
			Inbound = 0,
			/// <summary>
			/// The Outbound Queue is a queue of messages that have been queued for relaying.
			/// </summary>
			OutboundWaiting = 1
		}
	}
}
