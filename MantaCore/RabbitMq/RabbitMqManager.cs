using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MantaMTA.Core.RabbitMq
{
	public static class RabbitMqManager
	{
		/// <summary>
		/// Name of the Exchange in RabbitMQ to route dead letters from the wait* queues back in to the waiting queue.
		/// </summary>
		private const string MANTA_WAIT_DEAD_LETTER_EXCHANGE = "manta_mta_wait_exchange";

		/// <summary>
		/// The routing key to use for routing dead messages back into the waiting queue.
		/// </summary>
		private const string MANTA_WAIT_DEAD_LETTER_EXCHANGE_ROUTING_KEY = "manta_outbound_wait_route";

		/// <summary>
		/// The connection to the RabbitMQ instance.
		/// </summary>
		private static IConnection LocalhostConnection = new ConnectionFactory
		{
			HostName = MtaParameters.RabbitMQ.Hostname,
			UserName = MtaParameters.RabbitMQ.Username,
			Password = MtaParameters.RabbitMQ.Password
		}.CreateConnection();

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
		/// Channel used for publishing.
		/// </summary>
		public class PublishChannel
		{
			/// <summary>
			/// The Channel.
			/// </summary>
			private IModel _Channel = null;

			/// <summary>
			/// The Channel.
			/// </summary>
			public IModel Channel { get { return _Channel; } }

			/// <summary>
			/// Lock for the Channel.
			/// </summary>
			public object Lock = new object();

			public PublishChannel(IModel channel)
			{
				_Channel = channel;
			}
		}

		/// <summary>
		/// Collection of Channels for Publishing.
		/// </summary>
		public static Dictionary<int, PublishChannel> _PublishChannels = new Dictionary<int, PublishChannel>();

		/// <summary>
		/// Lock for getting a publish channel.
		/// </summary>
		public static object _PublishChannelsLock = new object();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="queue"></param>
		/// <returns></returns>
		public static PublishChannel GetPublishChannel(RabbitMqQueue queue, bool noConfirm)
		{
			IModel channel = GetChannel(queue); // Ensure the Queue exists.


			if (noConfirm)
				return new PublishChannel(channel);

			int threadID = Thread.CurrentThread.ManagedThreadId;

			lock (_PublishChannelsLock)
			{
				if (!_PublishChannels.ContainsKey(threadID))
				{
					PublishChannel pChannel = new PublishChannel(LocalhostConnection.CreateModel());
					pChannel.Channel.ConfirmSelect();
					_PublishChannels.Add(threadID, pChannel);
				}
			}

			return _PublishChannels[threadID];
		}


		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static bool Publish(byte[] message, RabbitMqQueue queue, bool noConfirm)
		{
			PublishChannel pChannel = GetPublishChannel(queue, noConfirm);
			lock (pChannel.Lock)
			{
				IModel channel = pChannel.Channel;
				IBasicProperties msgProps = channel.CreateBasicProperties();
				msgProps.Persistent = true;
				channel.BasicPublish(string.Empty, GetQueueNameFromEnum(queue), true, msgProps, message);
				if (noConfirm)
					return true;

				if (!channel.WaitForConfirms())
					return false;

				return true;
			}
		}

		/// <summary>
		/// Publishes the specified message to the specified queue.
		/// </summary>
		/// <param name="message">Message to queue.</param>
		/// <param name="queue">Queue to place message in.</param>
		public static bool Publish(object obj, RabbitMqQueue queue, bool confirm = true)
		{
			byte[] bytes = Serialisation.Serialise(obj);
			return Publish(bytes, queue, !confirm);
		}

		/// <summary>
		/// Gets the RabbitMQ queue name for the specified queue.
		/// </summary>
		/// <param name="queue">Queue to get the name of.</param>
		/// <returns>The name of the queue.</returns>
		private static string GetQueueNameFromEnum(RabbitMqQueue queue)
		{
			const string manta_queue_prefix = "manta_mta_";

			switch (queue)
			{
				case RabbitMqQueue.Inbound:
					return manta_queue_prefix + "inbound";
				case RabbitMqQueue.InboundStaging:
					return manta_queue_prefix + "inbound_staging";
				case RabbitMqQueue.OutboundWaiting:
					return manta_queue_prefix + "outbound_waiting";
				case RabbitMqQueue.OutboundWait1:
					return manta_queue_prefix + "outbound_wait_____1";
				case RabbitMqQueue.OutboundWait10:
					return manta_queue_prefix + "outbound_wait____10";
				case RabbitMqQueue.OutboundWait60:
					return manta_queue_prefix + "outbound_wait____60";
				case RabbitMqQueue.OutboundWait300:
					return manta_queue_prefix + "outbound_wait___300";
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
					if (!LocalhostConnection.IsOpen)
						LocalhostConnection = new ConnectionFactory
						{
							HostName = MtaParameters.RabbitMQ.Hostname,
							UserName = MtaParameters.RabbitMQ.Username,
							Password = MtaParameters.RabbitMQ.Password
						}.CreateConnection();

					channel = LocalhostConnection.CreateModel();
					Dictionary<string, object> queueArgs = null;
					bool isOutboundWaitingQueue = false;

					switch(queue)
					{
						case RabbitMqQueue.OutboundWait1:
						case RabbitMqQueue.OutboundWait10:
						case RabbitMqQueue.OutboundWait60:
						case RabbitMqQueue.OutboundWait300:
							isOutboundWaitingQueue = true;
							channel.ExchangeDeclare(MANTA_WAIT_DEAD_LETTER_EXCHANGE,	// Name of the Exchange to declare.
													"direct",							// The exchange type.
													true,								// Exchange is durable.
													false,								// Don't want the exchange auto deleted.
													null);								// No additional arguments.

							
							// The amount of time in milliseconds to allow messages to live in the queue.
							int messageTTL = 0;
							
							// Work out the TTL. 
							if (queue == RabbitMqQueue.OutboundWait1)
								messageTTL = 1 * 1000;
							else if (queue == RabbitMqQueue.OutboundWait10)
								messageTTL = 10 * 1000;
							else if (queue == RabbitMqQueue.OutboundWait60)
								messageTTL = 60 * 1000;
							else if (queue == RabbitMqQueue.OutboundWait300)
								messageTTL = 300 * 1000;

							// We are creating a queue with additional arguments for dead letter routing to the OutboundWaiting queue so set them here.
							queueArgs = new Dictionary<string, object>();
							queueArgs.Add("x-message-ttl", messageTTL);
							queueArgs.Add("x-dead-letter-exchange", MANTA_WAIT_DEAD_LETTER_EXCHANGE);
							queueArgs.Add("x-dead-letter-routing-key", MANTA_WAIT_DEAD_LETTER_EXCHANGE_ROUTING_KEY);
							break;
					}
					
					// Declare the Queue in RabbitMQ.
					channel.QueueDeclare(GetQueueNameFromEnum(queue),		// The Queue to use.
											true,							// True as we want the queue durable.
											false,							// Queue isn't exclusive.
											false,							// Don't auto delete from the queue.
											queueArgs);						// Add any additional Queue arguments.

					// If we are getting a channel to an Outbound Wait X queue, then we need to bind the dead letter exchange and the OutboundWaiting queue.
					if (isOutboundWaitingQueue)
						channel.QueueBind(GetQueueNameFromEnum(RabbitMqQueue.OutboundWaiting), 
										  MANTA_WAIT_DEAD_LETTER_EXCHANGE, 
										  MANTA_WAIT_DEAD_LETTER_EXCHANGE_ROUTING_KEY);

					_Channels[queue] = channel;
				}

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
					if(queue == RabbitMqQueue.Inbound)
						channel.BasicQos(0, 300, false);
					else
						channel.BasicQos(0, 750, false);

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
			OutboundWaiting = 1,
			/// <summary>
			/// Outbound wait queue, messages live here for one second before routing to OutboundWaiting.
			/// </summary>
			OutboundWait1 = 2,
			/// <summary>
			/// Outbound wait queue, messages live here for one minute before routing to OutboundWaiting.
			/// </summary>
			OutboundWait60 = 3,
			/// <summary>
			/// Outbound wait queue, messages live here for five minutes before routing to OutboundWaiting.
			/// </summary>
			OutboundWait300 = 4,
			OutboundWait10 = 5,
			InboundStaging = 6
		}
	}
}
