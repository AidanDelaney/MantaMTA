using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using Colony101.MTA.Library.DAL;

namespace Colony101.MTA.Library.Client.BO
{
	/// <summary>
	/// 
	/// </summary>
	internal class MtaMessage
	{
		/// <summary>
		/// Unique identifier for this message.
		/// </summary>
		public Guid ID { get; set; }
		/// <summary>
		/// The Mail From to used when sending this message.
		/// </summary>
		public MailAddress MailFrom { get; set; }
		/// <summary>
		/// Array of Rcpt To's for this message.
		/// </summary>
		public MailAddress[] RcptTo { get; set; }
		/// <summary>
		/// The path to the file containing this messages DATA.
		/// </summary>
		public string DataPath { get; set; }
		/// <summary>
		/// The IP address used to send this Message.
		/// </summary>
		public string OutboundIP { get; set; }
		/// <summary>
		/// The DATA for this message. Is read from <paramref name="DataPath"/>
		/// </summary>
		public string Data
		{
			get
			{
				if (string.IsNullOrWhiteSpace(this.DataPath))
					return string.Empty;

				using (StreamReader reader = new StreamReader(this.DataPath))
				{
					return reader.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// Save this MTA message to the Database.
		/// </summary>
		public void Save()
		{
			MtaMessageDB.Save(this);
		}

		/// <summary>
		/// Delete the DATA for this message.
		/// </summary>
		public void DeleteMessageData()
		{
			if (File.Exists(this.DataPath))
			{
				File.Delete(this.DataPath);
				this.DataPath = string.Empty;
				this.Save();
			}
		}

		/// <summary>
		/// Creates and MTA message from the passed in parameters. Also creates the messages data file and saves to database.
		/// </summary>
		/// <param name="outboundIP">IP Address message is/was sent from.</param>
		/// <param name="mailFrom">Mail From used in SMTP.</param>
		/// <param name="rcptTo">Rcpt To's used in SMTP.</param>
		/// <param name="data">Data used in SMTP.</param>
		/// <returns></returns>
		public static MtaMessage Create(string outboundIP, string mailFrom, string[] rcptTo, string data)
		{
			MtaMessage mtaMessage = new MtaMessage();
			mtaMessage.ID = Guid.NewGuid();
			mtaMessage.DataPath = Path.Combine(MtaParameters.MTA_QUEUEFOLDER, mtaMessage.ID + ".eml");
			if (mailFrom != null)
				mtaMessage.MailFrom = new MailAddress(mailFrom);
			else
				mtaMessage.MailFrom = null;

			mtaMessage.RcptTo = new MailAddress[rcptTo.Length];
			for (int i = 0; i < rcptTo.Length; i++)
				mtaMessage.RcptTo[i] = new MailAddress(rcptTo[i]);

			mtaMessage.OutboundIP = outboundIP;

			using (StreamWriter writer = new StreamWriter(mtaMessage.DataPath))
			{
				writer.Write(data);
			}

			mtaMessage.Save();

			return mtaMessage;
		}

		/// <summary>
		/// Queue the message.
		/// </summary>
		/// <returns></returns>
		public virtual MtaQueuedMessage Queue()
		{
			MtaQueuedMessage qMsg = new MtaQueuedMessage(this, DateTime.Now, DateTime.Now, false);
			MtaMessageDB.Save(qMsg);
			return qMsg;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	internal class MtaMessageCollection : List<MtaMessage>
	{
		public MtaMessageCollection(IEnumerable<MtaMessage> collection) : base(collection) { }
		public MtaMessageCollection() { }
	}
}
