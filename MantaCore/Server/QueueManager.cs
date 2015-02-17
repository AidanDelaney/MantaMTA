using MantaMTA.Core.Client.BO;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MantaMTA.Core.Server
{
	public class QueueManager : IStopRequired
	{
		private static QueueManager _Instance = new QueueManager();
		public static QueueManager Instance { get { return _Instance; } }
		private QueueManager() { }

		/// <summary>
		/// The maximum time between loooking for messages that have been queued in RabbitMQ.
		/// </summary>
		private const int RABBITMQ_MAX_TIME_IN_QUEUE = 5 * 1000;

		/// <summary>
		/// Enqueues the Inbound Message for Relaying.
		/// </summary>
		/// <param name="messageID">ID of the Message being Queued.</param>
		/// <param name="ipGroupID">ID of the Virtual MTA Group to send the Message through.</param>
		/// <param name="internalSendID">ID of the Send the Message is apart of.</param>
		/// <param name="mailFrom">The envelope mailfrom, should be return-path in most instances.</param>
		/// <param name="rcptTo">The envelope rcpt to.</param>
		/// <param name="message">The Email.</param>
		/// <returns>True if the Message has been queued, false if not.</returns>
		public async Task<bool> EnqueueAsync(Guid messageID, int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			// Try to queue the message in RabbitMQ.
			if (MtaParameters.RabbitMQ.IsEnabled && RabbitMq.RabbitMqInboundQueueManager.Enqueue(messageID, ipGroupID, internalSendID, mailFrom, rcptTo, message))
				return true;

			// If we failed to queue in RabbitMQ there must be something wrong so try to go to SQL.
			return await EnqueueSqlAsync(messageID, ipGroupID, internalSendID, mailFrom, rcptTo, message);
		}

		/// <summary>
		/// Enqueues the Inbound Message in SQL Server.
		/// </summary>
		/// <param name="messageID">ID of the Message being Queued.</param>
		/// <param name="ipGroupID">ID of the Virtual MTA Group to send the Message through.</param>
		/// <param name="internalSendID">ID of the Send the Message is apart of.</param>
		/// <param name="mailFrom">The envelope mailfrom, should be return-path in most instances.</param>
		/// <param name="rcptTo">The envelope rcpt to.</param>
		/// <param name="message">The Email.</param>
		/// <returns>True if the Message has been queued, false if not.</returns>
		private async Task<bool> EnqueueSqlAsync(Guid messageID, int ipGroupID, int internalSendID, string mailFrom, string[] rcptTo, string message)
		{
			// Need to put this message in the database for relaying to pickup
			return await MantaMTA.Core.Client.MessageSenderSql.Instance.EnqueueAsync(messageID, ipGroupID, internalSendID, mailFrom, rcptTo, message);
		}

		/// <summary>
		/// Thread used for copying data from RabbitMQ to SQL Server.
		/// </summary>
		private Thread _bulkInsertThread = null;

		/// <summary>
		/// Will be set to true when the Stop() method is called.
		/// </summary>
		private bool _isStopping = false;

		/// <summary>
		/// Will be set to true when the _bulkInsertThread has stopped.
		/// </summary>
		private bool _hasStopped = false;

		/// <summary>
		/// Start the bulk importer.
		/// </summary>
		public void Start()
		{
			if (MtaParameters.RabbitMQ.IsEnabled)
			{
				_bulkInsertThread = new Thread(new ThreadStart(DoSqlBulkInsertFromRabbitMQ));
				_bulkInsertThread.IsBackground = true;
				_bulkInsertThread.Start();
				//MantaCoreEvents.RegisterStopRequiredInstance(_Instance);
			}
			else
			{
				// Nothing to Start or Stop if not using RabbitMQ.
				_hasStopped = true;
			}
		}

		/// <summary>
		/// Stop the bulk importer.
		/// </summary>
		public void Stop()
		{
			Logging.Info("Stopping Bulk Inserter");
			_isStopping = true;

			int count = 0;
			while (!_hasStopped)
			{
				if(count > 50)
				{
					Logging.Error("Failed to stop Bulk Inserter");
					return;
				}
				Thread.Sleep(100);
				count++;
			}

			Logging.Info("Stopped Bulk Inserter");
		}

		/// <summary>
		/// Does the actual bulk importing from RabbitMQ to SQL Server.
		/// </summary>
		private void DoSqlBulkInsertFromRabbitMQ()
		{
			// Keep going until Manta is stopping.
			while(!_isStopping)
			{
				try
				{
					
					// Get queued messages for bulk importing.
					MtaMessageCollection recordsToImportToSql = RabbitMq.RabbitMqInboundQueueManager.Dequeue(50);
					
					// If there are no messages to import then sleep and try again.
					if(recordsToImportToSql == null || recordsToImportToSql.Count == 0)
					{
						Thread.Sleep(RABBITMQ_MAX_TIME_IN_QUEUE);
						continue;
					}

					// Do the SQL Import
					StringBuilder sbMessageValues = new StringBuilder(string.Empty);
					StringBuilder sbQueueValues = new StringBuilder(string.Empty);

					using(SqlConnection conn = DAL.MantaDB.GetSqlConnection())
					{
						SqlCommand cmd = conn.CreateCommand();
						string datetimenow = "@datetimenow";
						for(int i = 0; i < recordsToImportToSql.Count; i++)
						{
							string mta_msg_id = "@mta_msg_id" + i;
							string mta_send_internalId = "@mta_send_internalId" + i;
							string mta_msg_mailFrom = "@mta_msg_mailFrom" + i;
							string mta_msg_rcptTo = "@mta_msg_rcptTo" + i;
							string ip_group_id = "@ip_group_id" + i;
							string mta_queue_data = "@mta_queue_data" + i;


							string lastChar = (recordsToImportToSql.Count - 1) == i ? string.Empty : ",";
							sbMessageValues.AppendFormat("({0}, {1}, {2}, {3}){4}", mta_msg_id, mta_send_internalId, mta_msg_mailFrom, mta_msg_rcptTo, lastChar);
							
							cmd.Parameters.AddWithValue(mta_msg_id, recordsToImportToSql[i].MessageID);
							cmd.Parameters.AddWithValue(mta_send_internalId, recordsToImportToSql[i].InternalSendID);
							cmd.Parameters.AddWithValue(mta_msg_mailFrom, recordsToImportToSql[i].MailFrom);
							cmd.Parameters.AddWithValue(mta_msg_rcptTo, recordsToImportToSql[i].RcptTo[0]);
							cmd.Parameters.AddWithValue(ip_group_id, recordsToImportToSql[i].VirtualMTAGroupID);
						}

						cmd.CommandText = string.Format(@"
BEGIN TRANSACTION

INSERT INTO man_mta_msg(mta_msg_id, mta_send_internalId, mta_msg_mailFrom, mta_msg_rcptTo)
VALUES {0}

COMMIT TRANSACTION", sbMessageValues.ToString());

						cmd.Parameters.AddWithValue(datetimenow, DateTime.UtcNow);
						cmd.CommandTimeout = 5 * 60 * 1000;
						conn.Open();
						try
						{
							// Create a record of the messages in SQL server.
							cmd.ExecuteNonQuery();

							// Queue the messages in the RabbitMQ outbound queue.
							RabbitMq.RabbitMqOutboundQueueManager.Enqueue(recordsToImportToSql);
						}
						catch(Exception ex)
						{
							Logging.Warn("Server Queue Manager", ex);
						}
					}

					
				}
				catch(Exception ex)
				{
					Logging.Error("Bulk Importer Error", ex);
				}
			}

			_hasStopped = true;
		}
	}
}
