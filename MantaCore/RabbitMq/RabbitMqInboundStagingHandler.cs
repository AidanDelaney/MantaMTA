using MantaMTA.Core.Client.BO;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MantaMTA.Core.RabbitMq
{
	public static class RabbitMqInboundStagingHandler
	{
		public const int STAGING_DEQUEUE_THREADS = 25;
		public static int _StartedThreads = 0;

		public static void Start()
		{
			for (int i = 0; i < STAGING_DEQUEUE_THREADS; i++)
			{
				Thread t = new Thread(new ThreadStart(HandleDequeue));
				t.IsBackground = true;
				t.Start();
			}
		}

		private static void HandleDequeue()
		{
			if (_StartedThreads >= STAGING_DEQUEUE_THREADS)
				return;

			_StartedThreads++;

			while(true)
			{
				BasicDeliverEventArgs ea = RabbitMq.RabbitMqManager.Dequeue(RabbitMqManager.RabbitMqQueue.InboundStaging, 1, 100).FirstOrDefault();
				if(ea == null)
				{
					Thread.Sleep(1000);
					continue;
				}

				MtaQueuedMessage qmsg = Serialisation.Deserialise<MtaQueuedMessage>(ea.Body);
				MtaMessage msg = new MtaMessage(qmsg.ID, qmsg.VirtualMTAGroupID, qmsg.InternalSendID, qmsg.MailFrom, qmsg.RcptTo, string.Empty);

				RabbitMqManager.Publish(msg, RabbitMqManager.RabbitMqQueue.Inbound, true);
				RabbitMqManager.Publish(qmsg, RabbitMqManager.RabbitMqQueue.OutboundWaiting, true);
				RabbitMqManager.Ack(RabbitMqManager.RabbitMqQueue.InboundStaging, ea.DeliveryTag, false);
			}
		}
	}
}
