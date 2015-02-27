using MantaMTA.Core.Client.BO;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
				_bulkInsertThread.Priority = ThreadPriority.AboveNormal;
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
				if(count > 100)
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
					MtaMessageCollection recordsToImportToSql = RabbitMq.RabbitMqInboundQueueManager.Dequeue(100);
					
					// If there are no messages to import then sleep and try again.
					if(recordsToImportToSql == null || recordsToImportToSql.Count == 0)
					{
						Thread.Sleep(RABBITMQ_MAX_TIME_IN_QUEUE);
						continue;
					}

					DataTable dt = new DataTable();
					dt.Columns.Add("mta_msg_id", typeof(Guid));
					dt.Columns.Add("mta_send_internalId", typeof(int));
					dt.Columns.Add("mta_msg_rcptTo", typeof(string));
					dt.Columns.Add("mta_msg_mailFrom", typeof(string));

					using(SqlConnection conn = DAL.MantaDB.GetSqlConnection())
					{
						// Create a record of the messages in SQL server.
						using (SqlConnection conn = DAL.MantaDB.GetSqlConnection())
						{
							SqlBulkCopy bulk = new SqlBulkCopy(conn);
							bulk.DestinationTableName = "man_mta_msg_staging";
							foreach (DataColumn c in dt.Columns)
								bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

							conn.Open();
							bulk.WriteToServer(dt);
							SqlCommand cmd = conn.CreateCommand();
							cmd.CommandText = @"
BEGIN TRANSACTION
MERGE man_mta_msg AS target
    USING (SELECT * FROM man_mta_msg_staging) AS source
    ON (target.[mta_msg_id] = source.[mta_msg_id])
	WHEN NOT MATCHED THEN
		INSERT ([mta_msg_id], [mta_send_internalId], [mta_msg_rcptTo], [mta_msg_mailFrom])
		VALUES (source.[mta_msg_id], source.[mta_send_internalId], source.[mta_msg_rcptTo],  source.[mta_msg_mailFrom]);

DELETE FROM [man_mta_msg_staging]
COMMIT TRANSACTION";
							cmd.ExecuteNonQuery();
						}

						RabbitMq.RabbitMqOutboundQueueManager.Enqueue(recordsToImportToSql);
					}
					catch(Exception ex)
					{
						Logging.Warn("Server Queue Manager", ex);
					}
				}
				catch(Exception ex)
				{
					//Logging.Error("Bulk Importer Error", ex);
				}
			}

			_hasStopped = true;
		}
	}
}
