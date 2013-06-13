using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Enums;

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

		/// <summary>
		/// Holds the Transport MIME used to receive the Data message.
		/// </summary>
		public SmtpTransportMIME TransportMIME { get; set; }

		public SmtpTransaction()
		{
			RcptTo = new List<string>();
			MessageDestination = Enums.MessageDestination.Unknown;
			_hasMailFrom = false;
			Data = string.Empty;
			// Default value is set to 8bit as nearly all messages are sent using it.
			// Also some clients will send 8bit messages without passing a BODY parameter.
			TransportMIME = SmtpTransportMIME._8BitUTF;
		}

		/// <summary>
		/// Adds a header to the message data.
		/// </summary>
		/// <param name="name">The header name.</param>
		/// <param name="value">Value for the header.</param>
		public void AddHeader(string name, string value)
		{
			MessageHeaderCollection headers = MessageHeaderManager.GetMessageHeaders(Data);
			headers.Insert(0, new MessageHeader(name, value));
			Data = MessageHeaderManager.ReplaceHeaders(Data, headers);
		}

		/// <summary>
		/// Save message(s) to DROP folder. Will place files in rcpt sub folder.
		/// OR
		/// Add message to queue for delivery (relay).
		/// </summary>
		public void Save()
		{
			if (MessageDestination == Enums.MessageDestination.Self)
			{
				// The message is for local delivery

				// Add the MAIL FROM & RCPT TO headers.
				MessageHeaderCollection headers = MessageHeaderManager.GetMessageHeaders(Data);
				headers.Insert(0, new MessageHeader("X-Reciepient", string.Join("; ", RcptTo)));
				headers.Insert(0, new MessageHeader("X-Sender", MailFrom));
				Data = MessageHeaderManager.ReplaceHeaders(Data, headers);
				
				// Need to drop a copy of the message for each recipient.
				for (int i = 0; i < RcptTo.Count; i++)
				{
					// Put the messages in a subfolder for each recipient.
					string mailDirPath = Path.Combine(MtaParameters.MTA_DROPFOLDER, RcptTo[i]);

					// Ensure the directory exists by always calling create.
					Directory.CreateDirectory(mailDirPath);

					// Write the Email File.
					using (StreamWriter sw = new StreamWriter(Path.Combine(mailDirPath, Guid.NewGuid().ToString()) + ".eml"))
					{
						sw.Write(Data);
					}
				}
			}
			else if (MessageDestination == Enums.MessageDestination.Relay)
			{
				// The email is for relaying.

				// Look for any MTA control headers.
				MessageHeaderCollection headers = MessageHeaderManager.GetMessageHeaders(Data);

				// Will not be null if the SendGroupID header was present.
				MessageHeader ipGroupHeader = headers.SingleOrDefault(m => m.Name.Equals(MessageHeaderNames.SendGroupID, StringComparison.OrdinalIgnoreCase));

				// Parameter will hold the MtaIPGroup that will be used to relay this message.
				MtaIpAddress.MtaIPGroup mtaGroup = null;
				int ipGroupID = 0;
				if (ipGroupHeader != null)
				{
					if(int.TryParse(ipGroupHeader.Value, out ipGroupID))
						mtaGroup = MtaIpAddress.IpAddressManager.GetMtaIPGroup(ipGroupID);
				}

				// If the MTA group doesn't exist or it's not got any IPs, use the default.
				if (mtaGroup == null || 
					mtaGroup.IpAddresses.Count == 0)
					ipGroupID = MtaIpAddress.IpAddressManager.GetDefaultMtaIPGroup().ID;

				// Need to put this message in the database for relaying to pickup
				SmtpClient.Enqueue(ipGroupID, MailFrom, RcptTo.ToArray(), Data);
			}
			else
				throw new Exception("MessageDestination not set.");

		}
	}
}
