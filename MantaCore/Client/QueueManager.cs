using System;
using System.Collections.Concurrent;
using System.Threading;
using MantaMTA.Core.Client.BO;

namespace MantaMTA.Core.Client
{
	internal class QueueManager : IStopRequired
	{
		#region Singleton
		public static QueueManager Instance { get { return _Instance; } }
		private static QueueManager _Instance = new QueueManager();
		private QueueManager()
		{
			MantaCoreEvents.RegisterStopRequiredInstance(this);
			StartQueueFiller();
		}
		#endregion

		/// <summary>
		/// When set to TRUE signifies that the MTA is stopping.
		/// </summary>
		private bool _IsStopping = false;

		/// <summary>
		/// The maximum size of the memory queue.
		/// </summary>
		private const int MAX_QUEUE_SIZE = 100;

		/// <summary>
		/// THE QUEUE.
		/// </summary>
		private ConcurrentQueue<MtaQueuedMessage> _MemoryQueue = new ConcurrentQueue<MtaQueuedMessage>();

		/// <summary>
		/// Queue filler thread.
		/// </summary>
		private Thread _QueueFillerThread = null;

		/// <summary>
		/// Called to Stop the Message Queue instance.
		/// </summary>
		public void Stop()
		{
			_IsStopping = true;


			// Ensure disposal of messages in the queue.
			MtaQueuedMessage msg = null;
			while (_MemoryQueue.TryDequeue(out msg))
			{
				msg.Dispose();
			}
		}

		/// <summary>
		/// Gets a message from the queue.
		/// </summary>
		/// <returns>Queued message or NULL</returns>
		public MtaQueuedMessage GetMessageForSending()
		{
			MtaQueuedMessage qMsg = null;
			if (_MemoryQueue.TryDequeue(out qMsg))
				return qMsg;

			return null;
		}

		/// <summary>
		/// Starts the thread that is responsable for filling the in memory queue.
		/// </summary>
		private void StartQueueFiller()
		{
			if (_QueueFillerThread != null)
			{
				if(_QueueFillerThread.ThreadState != ThreadState.Stopped)
					return; // Thread is already running so nothing to do.
			}

			
			_QueueFillerThread = new Thread(new ThreadStart(delegate() 
			{ 
				try
				{
					// Keep the thread running until stop is requested.
					while (!_IsStopping)
					{
						// This will be set to true if we get messages from the database.
						bool pickedUpMessages = false;

						// If the MAX_QUEUE_SIZE is greater than the memory queue, then need to get more to fill it.
						if (_MemoryQueue.Count < MAX_QUEUE_SIZE)
						{
							// Get messages from the database queue.
							MtaQueuedMessageCollection messages = DAL.MtaMessageDB.PickupForSending(MAX_QUEUE_SIZE - _MemoryQueue.Count);
							
							// Set to true if we have messages from the database.
							pickedUpMessages = (messages.Count > 0);
							
							// Loop through the messages we just got.
							for (int i = 0; i < messages.Count; i++)
							{
								if (_IsStopping)
									// Stop was request while we were filling the queue.
									// We should stop filling the queue and just dispose of it.
									messages[i].Dispose();
								else
									// Enqueue the message into the memory queue.
									_MemoryQueue.Enqueue(messages[i]);
							}
						}

						// If we have filled up the queue sleep for a bit.
						if (_MemoryQueue.Count == MAX_QUEUE_SIZE)
							Thread.Sleep(250);
						// If there were no more messages in the database sleep for a bit longer.
						else if (!pickedUpMessages)
							Thread.Sleep(1000);
					}
				}
				catch (Exception ex)
				{
					// Something bad happened.
					Logging.Fatal("Queue Manager Stopped", ex);
					MantaCoreEvents.InvokeMantaCoreStopping();
					Environment.Exit(-1);
				}
			}));
			_QueueFillerThread.Start();
		}
	}
}
