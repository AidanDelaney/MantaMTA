using System;
using System.Collections.Generic;
using System.IO;
using Colony101.MTA.Library.Client;

namespace Colony101.MTA.Library.Server
{
	/// <summary>
	/// Represents an SMTP Transaction
	/// </summary>
	internal class SmtpTransaction
	{
		/// <summary>
		/// The destination for this message.
		/// This should be set to inform us if the message should be put in the drop folder.
		/// Or saved to the database for relaying.
		/// </summary>
		public Colony101.MTA.Library.Enums.MessageDestination MessageDestination { get; set; }

		/// <summary>
		/// The mail from.
		/// </summary>
		public string MailFrom
		{
			get
			{
				return _mailFrom;
			}
			set
			{
				_mailFrom = value;
				_hasMailFrom = true;
			}
		}
		public string _mailFrom { get; set; }
		/// <summary>
		/// FALSE until a MailFrom has been set.
		/// </summary>
		public bool HasMailFrom { get { return _hasMailFrom; } }
		private bool _hasMailFrom { get; set; }

		/// <summary>
		/// List of the recipients.
		/// </summary>
		public List<string> RcptTo { get; set; }

		/// <summary>
		/// The message data.
		/// </summary>
		public string Data { get; set; }

		public SmtpTransaction()
		{
			RcptTo = new List<string>();
			MessageDestination = Enums.MessageDestination.Unknown;
			_hasMailFrom = false;
			Data = string.Empty;
		}

		public void SetHeaders(string receivedFrom)
		{
			// If the Data doesn't have a header section make sure to add it with appropriate headers
			if (Data.IndexOf("\r\n\r\n") < 0)
			{
				//Data = "From: <" + MailFrom + ">" + Environment.NewLine +
				//	   "To: " + GetRcptAddresses() + Environment.NewLine + Environment.NewLine + Data;
				Data = Environment.NewLine + Data;
			}

			// Add the receivedFrom header
			Data = receivedFrom + Environment.NewLine + Data;
		}

		/// <summary>
		/// Save message(s) to DROP folder. Will place files in rcpt sub folder.
		/// OR
		/// Add message to queue for delivery (relay).
		/// </summary>
		public void Save(string destIP)
		{
			if (MessageDestination == Enums.MessageDestination.Self)
			{
				for (int i = 0; i < RcptTo.Count; i++)
				{
					string mailDirPath = Path.Combine(MtaParameters.MTA_DROPFOLDER, RcptTo[i]);
					Directory.CreateDirectory(mailDirPath);
					using (StreamWriter sw = new StreamWriter(Path.Combine(mailDirPath, Guid.NewGuid().ToString()) + ".eml"))
					{
						sw.Write(Data);
					}
				}
			}
			else if (MessageDestination == Enums.MessageDestination.Relay)
			{
				// Need to put this message in the database for relaying to pickup
				SmtpClient.Enqueue(destIP, MailFrom, RcptTo.ToArray(), Data);
			}
			else
				throw new Exception("MessageDestination not set.");

		}
	}
}
