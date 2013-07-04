using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using MantaMTA.Core.Message;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// Handles events such as Abuse and Bounces as a result of emails being sent.
	/// </summary>
	public class EventsManager
	{
		/// <summary>
		/// Holds a singleton instance of the EventsManager.
		/// </summary>
		public static EventsManager Instance { get { return _Instance; } }
		private static readonly EventsManager _Instance = new EventsManager();
		private EventsManager() { }


		/// <summary>
		/// Examines a string to identify detailed bounce information from it.
		/// </summary>
		/// <param name="message">Could either be an entire email or just a multiple line response from another MTA.</param>
		/// <returns>A EmailProcessingResult value indicating the result.</returns>
		public EmailProcessingResult ProcessBounce(string message)
		{
			// Get some values from the emailContent.
			
			// return ProcessBounce(from, to, message);
			return EmailProcessingResult.Unknown;
		}

		/// <summary>
		/// Looks through a feedback look looking for a Manta-MTA return path. If found logs the event.
		/// 
		/// Look for return path in following order.
		/// Abuse Report Original-Mail-From.										[Yahoo]
		/// Return-Path from body part child with content-type of message/rfc822.	[AOL]
		/// Return-Path in message headers.											[Hotmail]
		/// </summary>
		/// <param name="message">The feedback look email.</param>
		public EmailProcessingResult ProcessFeedbackLoop(MimeMessage message)
		{
			try
			{
				// Look for abuse report
				MimeMessageBodyPart abuseReport = message.BodyParts.SingleOrDefault(bp => bp.ContentType.MediaType.Equals("message/feedback-report", StringComparison.OrdinalIgnoreCase));
				if (abuseReport != null)
				{
					string abuseReportBody = abuseReport.GetDecodedBody();
					abuseReportBody = MessageManager.UnfoldHeaders(abuseReportBody);
					using (StringReader reader = new StringReader(abuseReportBody))
					{
						while (reader.Peek() > -1)
						{
							string line = reader.ReadLine();
							if (line.StartsWith("Original-Mail-From:", StringComparison.OrdinalIgnoreCase))
							{
								string tmp = line.Substring("Original-Mail-From: ".Length - 1);
								try
								{
									int internalSendID = -1;
									string rcptTo = string.Empty;

									if (ReturnPathManager.TryDecode(tmp, out rcptTo, out internalSendID))
									{
										// NEED TO LOG TO DB HERE!!!!!
										return EmailProcessingResult.SuccessAbuse;
									}
								}
								catch (Exception)
								{
									// Must be redacted
									break;
								}
							}
						}
					}
				}

				Func<MessageHeaderCollection, bool> checkForReturnPathHeaders = new Func<MessageHeaderCollection, bool>(delegate(MessageHeaderCollection headers)
					{
						MessageHeader returnPathHeader = headers.SingleOrDefault(s => s.Name.Equals("Return-Path", StringComparison.OrdinalIgnoreCase));
						if (returnPathHeader != null &&
							!string.IsNullOrWhiteSpace(returnPathHeader.Value))
						{
							int internalSendID = -1;
							string rcptTo = string.Empty;

							if (ReturnPathManager.TryDecode(returnPathHeader.Value, out rcptTo, out internalSendID))
							{
								// NEED TO LOG TO DB HERE!!!!!
								return true;
							}
						}

						MessageHeader messageIdHeader = headers.SingleOrDefault(s => s.Name.Equals("Message-ID", StringComparison.OrdinalIgnoreCase));
						if (messageIdHeader != null &&
							messageIdHeader.Value.Length > 33)
						{
							string tmp = messageIdHeader.Value.Substring(1, 32);
							Guid messageID;
							if (Guid.TryParse(tmp, out messageID))
							{
								int internalSendID = -1;
								string rcptTo = string.Empty;

								tmp = ReturnPathManager.GetReturnPathFromMessageID(messageID);
								if (ReturnPathManager.TryDecode(tmp, out rcptTo, out internalSendID))
								{
									// NEED TO LOG TO DB HERE!!!!!
									return true;
								}
							}
						}

						return false;
					});

				// There isn't an abuse report or it doesn't contain the information we need.
				// Check for any body parts with child messages.
				MimeMessageBodyPart[] children = message.BodyParts.Where(bp => bp.HasChildMimeMessage).ToArray();
				if (children != null)
				{
					for (int i = 0; i < children.Length; i++)
					{
						MimeMessage child = children[i].ChildMimeMessage;
						if (checkForReturnPathHeaders(child.Headers))
							return EmailProcessingResult.SuccessAbuse;
					}
				}

				// There wasn't any child bodyparts or they didn't contain the info we need.
				// Look to see if our email has just been bounced to us.
				if(checkForReturnPathHeaders(message.Headers))
					return EmailProcessingResult.SuccessAbuse;
			}
			catch (Exception) { }

			Logging.Debug("Failed to find return path!");
			return EmailProcessingResult.ErrorNoReturnPath;
		}
	}
}
