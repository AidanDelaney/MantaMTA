using MantaMTA.Core.Client.BO;
using MantaMTA.Core.DAL;
using MantaMTA.Core.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MantaMTA.Core.Sends
{
	public class SendManager : ISendManagerContract
	{
		public static SendManager Instance
		{
			get
			{
				return SendManager._Instance;
			}
		}
		private static SendManager _Instance = new SendManager();
		private SendManager()
		{
		}

		/// <summary>
		/// ServiceHost to host the service contact.
		/// </summary>
		private ServiceHost _ServiceHost = null;

		/// <summary>
		/// Collection of cached Sends, key is SendID.
		/// Objects should be in this and _SendsInternalID with alternate key.
		/// </summary>
		private ConcurrentDictionary<string, Send> _Sends = new ConcurrentDictionary<string, Send>();

		/// <summary>
		/// Collection of cached Sends, key is internal ID.
		/// Objects should be in this and _Sends with alternate key.
		/// </summary>
		private ConcurrentDictionary<int, Send> _SendsInternalID = new ConcurrentDictionary<int, Send>();
		
		/// <summary>
		/// Timestamp of when cached sends were last cleared. 
		/// </summary>
		private DateTime _SendsLastCleared = DateTime.UtcNow;

		/// <summary>
		/// Sends cache lock, used when clearing the cached items.
		/// </summary>
		private object _SendsLock = new object();
		
		/// <summary>
		/// Gets Send with the specified sendID.
		/// If it doesn't exist it will be created in the database.
		/// </summary>
		/// <param name="sendId">ID of the Send.</param>
		/// <returns>The Send.</returns>
		public Send GetSend(string sendId)
		{
			// Don't want send IDs sitting in memory for to long so clear every so often.
			if (this._SendsLastCleared.AddSeconds(10) < DateTime.UtcNow)
				this.ClearSendsCache();

			Send snd;

			// Try to get the send id from the cached collection.
			if (!this._Sends.TryGetValue(sendId, out snd))
			{
				// Doesn't exist so need to create or load from datbase.
				snd = SendDB.CreateAndGetInternalSendID(sendId);

				// Add are new item to the cache.
				this._Sends.TryAdd(sendId, snd);
				this._SendsInternalID.TryAdd(snd.InternalID, snd);
			}

			// return the value.
			return snd;
		}

		/// <summary>
		/// Gets the specified Send.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		/// <returns>The Send.</returns>
		public Send GetSend(int internalSendID)
		{
			// Don't want send IDs sitting in memory for to long so clear every so often.
			if (this._SendsLastCleared.AddSeconds(10) < DateTime.UtcNow)
				this.ClearSendsCache();

			Send snd;

			// Try to get the send id from the cached collection.
			if (!this._SendsInternalID.TryGetValue(internalSendID, out snd))
			{
				// Doesn't exist so need to create or load from datbase.
				snd = SendDB.GetSend(internalSendID);

				// Add are new item to the cache.
				this._SendsInternalID.TryAdd(internalSendID, snd);
				this._Sends.TryAdd(snd.ID, snd);
			}

			return snd;
		}

		/// <summary>
		/// Gets the default send ID, based of the current time.
		/// </summary>
		/// <returns></returns>
		internal Send GetDefaultInternalSendId()
		{
			string sendID = DateTime.UtcNow.ToString("yyyyMMdd");
			return this.GetSend(sendID);
		}

		/// <summary>
		/// Clear the Sends from memory.
		/// </summary>
		public void ClearSendsCache()
		{
			lock (_SendsLock)
			{
				this._Sends.Clear();
				this._SendsInternalID.Clear();
				this._SendsLastCleared = DateTime.UtcNow;
				Logging.Info("Cleared Send Cache");
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
				MantaCoreEvents.InvokeMantaCoreStopping();
				Environment.Exit(-1);
			}));
			this._ServiceHost.Open();
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
			MtaQueuedMessageCollection messages = MtaMessageDB.PickupForDiscarding(25);
			while (messages.Count > 0)
			{
				Parallel.ForEach<MtaQueuedMessageSql>(messages, delegate(MtaQueuedMessageSql msg)
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
