using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;
using MantaMTA.Core.Client.BO;
using MantaMTA.Core.DAL;
using MantaMTA.Core.ServiceContracts;

namespace MantaMTA.Core.Sends
{
	public class SendManager : ISendManagerContract
	{
		public static SendManager Instance { get { return _Instance; } }
		private static SendManager _Instance = new SendManager();
		private SendManager() { }

		/// <summary>
		/// ServiceHost to host the service contact.
		/// </summary>
		private ServiceHost _ServiceHost = null;

		/// <summary>
		/// Collection of SendIDs
		/// Overtime this will get big so should be cleared every hourish.
		/// </summary>
		private ConcurrentDictionary<string, Send> _Sends = new ConcurrentDictionary<string, Send>();
		
		/// <summary>
		/// Timestamp of when _SendIDs was last cleared. 
		/// </summary>
		private DateTime _SendsLastCleared = DateTime.UtcNow;

		/// <summary>
		/// Gets the internal send ID to be used for the specified sendID.
		/// If it doesn't exist it will be created in the database.
		/// </summary>
		/// <param name="sendId"></param>
		/// <returns></returns>
		public Send GetSend(string sendId)
		{
			// Don't want send IDs sitting in memory for to long so clear every so often.
			if (_SendsLastCleared.AddHours(1) < DateTime.UtcNow)
				ClearSendsCache();

			Send snd;

			// Try to get the send id from the cached collection.
			if (!_Sends.TryGetValue(sendId, out snd))
			{
				// Doesn't exist so need to create or load from datbase.
				snd = DAL.SendDB.CreateAndGetInternalSendID(sendId);

				// Add are new item to the cache.
				_Sends.TryAdd(sendId, snd);
			}

			// return the value.
			return snd;
		}

		/// <summary>
		/// Gets the default send ID, based of the current time.
		/// </summary>
		/// <returns></returns>
		internal Send GetDefaultInternalSendId()
		{
			string sendID = DateTime.UtcNow.ToString("yyyyMMdd");
			return GetSend(sendID);
		}

		/// <summary>
		/// Clear the Sends from memory.
		/// </summary>
		public void ClearSendsCache()
		{
			lock (_Sends)
			{
				_Sends.Clear();
				_SendsLastCleared = DateTime.UtcNow;
			}
		}

		public void StartService()
		{
			_ServiceHost = ServiceContractManager.CreateServiceHost(typeof(SendManager),
													 typeof(ISendManagerContract),
													 ServiceContractManager.ServiceAddresses.SendManager,
													 new EventHandler(delegate(object sender, EventArgs e)
													 {
														 Logging.Fatal("SendManager ServiceHost Faulted");
														 Environment.Exit(-1);
													 }));
			_ServiceHost.Open();
		}

		/// <summary>
		/// Pause the specified send.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the send to pause.</param>
		public void Pause(int internalSendID)
		{
			SendDB.PauseSend(internalSendID);
			SendManager.Instance.ClearSendsCache();
		}

		/// <summary>
		/// Discards a send.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		public void Discard(int internalSendID)
		{
			SendDB.DiscardSend(internalSendID);
			SendManager.Instance.ClearSendsCache();
			SendManager.Instance.DiscardMessages(internalSendID);
		}

		/// <summary>
		/// Discards the messages in send queue.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		private void DiscardMessages(int internalSendID)
		{
			MtaQueuedMessageCollection messages = DAL.MtaMessageDB.PickupForDiscarding(25);
			while (messages.Count > 0)
			{
				Parallel.ForEach(messages, delegate(MtaQueuedMessage msg)
				{
					msg.HandleMessageDiscard();
				});

				messages = MtaMessageDB.PickupForDiscarding(25);
			}
		}

		/// <summary>
		/// Resumes a send. Sets it to active.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		public void Resume(int internalSendID)
		{
			SendDB.ResumeSend(internalSendID);
			SendManager.Instance.ClearSendsCache();
		}
	}
}
