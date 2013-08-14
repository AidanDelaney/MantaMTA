using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MantaMTA.Core.DAL;
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
		/// Class to store any re-usable Regex patterns.
		/// </summary>
		internal static class RegexPatterns
		{
			/// <summary>
			/// Regex pattern to grab an SMTP code (e.g. "550") and/or an NDR code (e.g. "5.1.1") as well as any detail that follows them.
			/// </summary>
			internal static string SmtpResponse = @"^(?<Detail>((?<SmtpCode>\d{3}|)(\s|-)|)(?<NdrCode>\d{1}\.\d{1,3}\.\d{1,3}|).*)$";

			/// <summary>
			/// Pattern to get a Non-Delivery Report code from a string.  They are in the format "x.y[1-3].z[1-3]".
			/// </summary>
			internal static string NonDeliveryReportCode = @"\b(?<NdrCode>\d{1}\.\d{1,3}\.\d{1,3})\b";
		}


		/// <summary>
		/// Examines an email to try identify detailed bounce information from it.
		/// </summary>
		/// <param name="message">The entire text content for an email (headers and body).</param>
		/// <returns>An EmailProcessingResult value indicating whether the the email was
		/// successfully processed or not.</returns>
		public EmailProcessingResult ProcessBounceEmail(string message)
		{
			MimeMessage msg = MimeMessage.Parse(message);

			if (msg == null)
				return EmailProcessingResult.ErrorContent;

			// "x-receiver" should contain what Manta originally set as the "return-path" when sending.
			MessageHeader returnPath = msg.Headers.GetFirstOrDefault("x-receiver");
			if (returnPath == null)
				return EmailProcessingResult.ErrorNoReturnPath;

			string rcptTo = string.Empty;
			int internalSendID = 0;

			if (!ReturnPathManager.TryDecode(returnPath.Value, out rcptTo, out internalSendID))
			{
				// Not a valid Return-Path so can't process.
				return EmailProcessingResult.SuccessNoAction;
			}

			MantaBounceEvent bounceEvent = new MantaBounceEvent();
			bounceEvent.EmailAddress = rcptTo;
			bounceEvent.SendID = MantaMTA.Core.DAL.SendDB.GetSend(internalSendID).ID;

			// TODO: Might be good to get the DateTime found in the email.
			bounceEvent.EventTime = DateTime.UtcNow;

			// These properties are both set according to the SMTP code we find, if any.
			bounceEvent.BounceInfo.BounceCode = MantaBounceCode.Unknown;
			bounceEvent.BounceInfo.BounceType = MantaBounceType.Unknown;
			bounceEvent.EventType = MantaEventType.Bounce;

			// First, try to find a NonDeliveryReport body part as that's the proper way for an MTA
			// to tell us there was an issue sending the email.

			BouncePair bouncePair;
			string bounceMsg;
			string deliveryReport = string.Empty;

			if (FindDeliveryReport(msg.BodyParts, out deliveryReport))
			{
				// If we've got a delivery report, check it for info.
			
				if (ParseNdr(deliveryReport, out bouncePair, out bounceMsg))
				{
					// Successfully parsed.
					bounceEvent.BounceInfo = bouncePair;
					bounceEvent.Message = bounceMsg;

					// Write BounceEvent to DB.
					Save(bounceEvent);

					return EmailProcessingResult.SuccessBounce;
				}
			}



			// We're still here so there was either no NDR part or nothing contained within it that we could
			// interpret so have to check _all_ body parts for something useful.
			if (FindBounceReason(msg.BodyParts, out bouncePair, out bounceMsg))
			{
				bounceEvent.BounceInfo = bouncePair;
				bounceEvent.Message = bounceMsg;

				// Write BounceEvent to DB.
				// TODO

				return EmailProcessingResult.SuccessBounce;
			}





			// Nope - no clues relating to why the bounce occurred.
			bounceEvent.BounceInfo.BounceType = MantaBounceType.Unknown;
			bounceEvent.BounceInfo.BounceCode = MantaBounceCode.Unknown;
			bounceEvent.Message = string.Empty;
			return EmailProcessingResult.Unknown;
		}


		/// <summary>
		/// Examines a non-delivery report for detailed bounce information.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="bounceType"></param>
		/// <param name="bounceCode"></param>
		/// <param name="bounceMessage"></param>
		/// <returns></returns>
		internal bool ParseNdr(string message, out BouncePair bouncePair, out string bounceMessage)
		{
			// Check for the Diagnostic-Code as hopefully contains more information about the error.
			const string DiagnosticCodeFieldName = "Diagnostic-Code: ";
			const string StatusFieldName = "Status: ";



			StringBuilder diagnosticCode = new StringBuilder(string.Empty);
			string status = string.Empty;

			using (StringReader sr = new StringReader(message))
			{
				string line = sr.ReadLine();

				// While the string reader has stuff to read keep looping through each line.
				while (!string.IsNullOrWhiteSpace(line) || sr.Peek() > -1)
				{
					if (line.StartsWith(DiagnosticCodeFieldName, StringComparison.OrdinalIgnoreCase))
					{
						// Found the diagnostic code.

						// Remove the field name.
						line = line.Substring(DiagnosticCodeFieldName.Length);

						// Check to see if the disagnostic-code contains an SMTP response.
						bool isSmtpResponse = line.StartsWith("smtp;", StringComparison.OrdinalIgnoreCase);
						
						// Add the first line of the diagnostic-code.
						diagnosticCode.AppendLine(line);

						// Will be set to true when we find the next non diagnostic-code line.
						bool foundNextLine = false;
						
						// Loop to read multiline diagnostic-code.
						while (!foundNextLine)
						{
							if (sr.Peek() == -1)
								break; // We've reached the end of the string!

							// Read the next line.
							line = sr.ReadLine();

							if (isSmtpResponse)
							{
								// Diagnostic code is an SMTP response so look for SMTP response line.
								if (Regex.IsMatch(line, @"\d{3}(-|\s)"))
									diagnosticCode.AppendLine(line);
								else // Not a SMTP response line so must be next NDR line.
									foundNextLine = true;
							}
							else
							{
								// Non SMTP response. If first char is whitespace then it's part of the disagnostic-code otherwise it isn't.
								if (char.IsWhiteSpace(line[0]))
									diagnosticCode.AppendLine(line);
								else
									foundNextLine = true;
							}
						}
					}
					else
					{
						// We haven't found a diagnostic-code line.
						// Check to see if we have found a status field.
						if (line.StartsWith(StatusFieldName, StringComparison.OrdinalIgnoreCase))
							status = line.Substring(StatusFieldName.Length).TrimEnd();

						// If there is more of the string to read then read the next line, otherwise set line to string.empty.
						if (sr.Peek() > -1)
							line = sr.ReadLine();
						else
							line = string.Empty;
					}
				}
			}


			// Process what we've managed to find...

			// Diagnostic-Code
			if (!string.IsNullOrWhiteSpace(diagnosticCode.ToString()))
			{
				if (ParseSmtpDiagnosticCode(diagnosticCode.ToString(), out bouncePair, out bounceMessage))
				{
					return true;
				}
			}


			// Status
			if (!string.IsNullOrWhiteSpace(status))
			{
				// If there's an NDR code in the Status value, use it.
				Match m = Regex.Match(status, RegexPatterns.NonDeliveryReportCode, RegexOptions.ExplicitCapture);

				if (m.Success)
				{
					bouncePair = BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair(m.Value);
					bounceMessage = m.Value;
					return true;
				}
			}




			// If we've not already returned from this method, then we're still looking for an explanation
			// for the bounce so parse the entire message as a string.
			if (ParseBounceMessage(message, out bouncePair, out bounceMessage))
				return true;
			


			// Nope - no clues relating to why the bounce occurred.
			bouncePair.BounceType = MantaBounceType.Unknown;
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bounceMessage = string.Empty;
			return false;
		}


		/// <summary>
		/// Examines an SMTP response that is thought to relate to delivery of an email failing.
		/// </summary>
		/// <param name="message">An SMTP response either found as the "Diagnostic-Code" value in a Non-Delivery Report
		/// or received directly from another MTA in an SMTP session.</param>
		/// <param name="bouncePair">out.  Details of the Bounce (or not) based on details found in <paramref name="message"/>.</param>
		/// <param name="bounceMessage">out.  The message found that indicated a Bounce or string.Empty if it wasn't
		/// found to indicate a bounce.</param>
		/// <returns>true if a bounce was positively identified, else false.</returns>
		internal bool ParseSmtpDiagnosticCode(string message, out BouncePair bouncePair, out string bounceMessage)
		{
			// Remove "smtp;[possible whitespace]" if it appears at the beginning of the message.
			if (message.StartsWith("smtp;", StringComparison.OrdinalIgnoreCase))
				message = message.Substring("smtp;".Length).Trim();

			if (ParseBounceMessage(message, out bouncePair, out bounceMessage))
				return true;

			
			return false;
		}


		/// <summary>
		/// Examines an SMTP response message to identify detailed bounce information from it.
		/// </summary>
		/// <param name="response">The message that's come back from an external MTA when attempting to send an email.</param>
		/// <param name="rcptTo">The email address that was being sent to.</param>
		/// <param name="internalSendID">The internal Manta SendID.</param>
		/// <returns>True if a bounce was found and recorded, false if not.</returns>
		internal bool ProcessSmtpResponseMessage(string response, string rcptTo, int internalSendID)
		{
			BouncePair bouncePair = new BouncePair();
			string bounceMessage = string.Empty;

			if (ParseBounceMessage(response, out bouncePair, out bounceMessage))
			{
				// Were able to find the bounce so create the bounce event.
				MantaBounceEvent bounceEvent = new MantaBounceEvent
				{
					EventType = MantaEventType.Bounce,
					EmailAddress = rcptTo,
					BounceInfo = bouncePair,
					SendID = SendDB.GetSend(internalSendID).ID,
					// It is possible that the bounce was generated a while back, but we're assuming "now" for the moment.
					// Might be good to get the DateTime found in the email at a later point.
					EventTime = DateTime.UtcNow,
					Message = response
				};

				// Log to DB.
				Save(bounceEvent);


				// All done return true.
				return true;
			}

			// Couldn't identify the bounce.
			return false;
		}


		/// <summary>
		/// Attempts to find the reason for the bounce by running Bounce Rules, then checking for Non-Delivery Report codes,
		/// and finally checking for SMTP codes.
		/// </summary>
		/// <param name="message">Could either be an email body part or a single or multiple line response from another MTA.</param>
		/// <param name="bouncePair">out.</param>
		/// <param name="bounceMessage">out.</param>
		/// <returns>true if a positive match in <paramref name="message"/> was found indicating a bounce, else false.</returns>
		internal bool ParseBounceMessage(string message, out BouncePair bouncePair, out string bounceMessage)
		{
			// Check all Bounce Rules for a match.
			foreach (BounceRule r in BounceRulesManager.BounceRules)
			{
				// If we get a match, we're done processing Rules.
				if (r.IsMatch(message, out bounceMessage))
				{
					bouncePair.BounceType = r.BounceTypeIndicated;
					bouncePair.BounceCode = r.BounceCodeIndicated;
					bounceMessage = message;
					return true;
				}
			}


			// No Bounce Rules match the message so try to get a match on an NDR code ("5.1.1") or an SMTP code ("550").
			// TODO: Handle several matches being found - somehow find The Best?
			// Pattern: Should match like this:
			//	[anything at the beginning if present][then either an SMTP code or an NDR code, but both should be grabbed if
			// they exist][then the rest of the content (if any)]
			Match match = Regex.Match(message, RegexPatterns.SmtpResponse, RegexOptions.Singleline | RegexOptions.ExplicitCapture);

			if (match.Success)
			{
				bounceMessage = match.Value;

				// Check for anything useful with the NDR code first as it contains more specific detail than the SMTP code.
				if (match.Groups["NdrCode"].Success && match.Groups["NdrCode"].Length > 0)
				{
					bouncePair = BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair(match.Groups["NdrCode"].Value);
					if (bouncePair.BounceCode != MantaBounceCode.General)
						return true;
				}

				// Try the SMTP code as there wasn't an NDR.
				if (match.Groups["SmtpCode"].Success && match.Groups["SmtpCode"].Length > 0)
				{
					bouncePair = BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(Int32.Parse(match.Groups["SmtpCode"].Value));
					return true;
				}
			}


			// Failed to identify a reason so shouldn't be a bounce.
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bouncePair.BounceType = MantaBounceType.Unknown;
			bounceMessage = string.Empty;

			return false;
		}


		/// <summary>
		// Attempts to retrieve a delivery report that appears within a body part of an email.
		/// </summary>
		/// <param name="bodyParts">An array of BodyParts to search within (including any child BodyParts).</param>
		/// <param name="report">out.  If a delivery report is found, this will contain the content of it,
		/// else string.Empty.</param>
		/// <returns>true if a delivery report was found, else false.</returns>
		internal bool FindDeliveryReport(BodyPart[] bodyParts, out string report)
		{
			foreach (BodyPart bp in bodyParts)
			{
				if (bp.ContentType.MediaType.Equals("message/delivery-status", StringComparison.OrdinalIgnoreCase))
				{
					// Found it!
					report = bp.GetDecodedBody();
					return true;
				}
				else if (bp.ContentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
				{
					// Loop through the child body parts.
					if (FindDeliveryReport(bp.BodyParts, out report))
						return true;
				}
				else
					// Ignore this BodyPart.
					continue;
			}


			// If we're still here, then we didn't find a delivery report.
			report = string.Empty;
			return false;
		}


		/// <summary>
		/// Attempts to find and process the reason an email bounced by digging through all body parts.
		/// </summary>
		/// <param name="bodyParts">Array of BodyParts to dig through; may include child body parts.</param>
		/// <param name="bouncePair">out.  A BouncePair object containing details of the bounce (if a reason was found).</param>
		/// <param name="bounceMessage">out.  The text to use as the reason found why the bounce occurred.</param>
		/// <returns>true if a reason was found, else false.</returns>
		internal bool FindBounceReason(BodyPart[] bodyParts, out BouncePair bouncePair, out string bounceMessage)
		{
			foreach(BodyPart b in bodyParts)
			{
				if (b.ContentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
				{
					// Just a container for other body parts so check them.
					if (FindBounceReason(b.BodyParts, out bouncePair, out bounceMessage))
						return true;
				}
				else if (b.ContentType.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
				{
					// Only useful to examine text/plain body parts (so not "image/gif" etc).
					if (ParseBounceMessage(b.GetDecodedBody(), out bouncePair, out bounceMessage))
						return true;
				}
				// else
				//	Console.WriteLine("\tSkipped bodypart \"" + b.ContentType.MediaType + "\".");
			}


			// Still here so haven't found anything useful.
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bouncePair.BounceType = MantaBounceType.Unknown;
			bounceMessage = string.Empty;
			return false;
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
		public EmailProcessingResult ProcessFeedbackLoop(string content)
		{
			MimeMessage message = MimeMessage.Parse(content);
			if (message == null)
				return EmailProcessingResult.ErrorContent;
			try
			{
				// Look for abuse report
				BodyPart abuseReport = message.BodyParts.SingleOrDefault(bp => bp.ContentType.MediaType.Equals("message/feedback-report", StringComparison.OrdinalIgnoreCase));
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
										Sends.Send snd = MantaMTA.Core.DAL.SendDB.GetSend(internalSendID);
										Save(new MantaAbuseEvent 
										{ 
											EmailAddress = rcptTo, 
											EventTime = DateTime.UtcNow, 
											EventType = MantaEventType.Abuse, 
											SendID = (snd == null ? string.Empty : snd.ID) 
										});
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
								Sends.Send snd = MantaMTA.Core.DAL.SendDB.GetSend(internalSendID);
								Save(new MantaAbuseEvent
								{
									EmailAddress = rcptTo,
									EventTime = DateTime.UtcNow,
									EventType = MantaEventType.Abuse,
									SendID = (snd == null ? string.Empty : snd.ID)
								});
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
									Sends.Send snd = MantaMTA.Core.DAL.SendDB.GetSend(internalSendID);
									Save(new MantaAbuseEvent
									{
										EmailAddress = rcptTo,
										EventTime = DateTime.UtcNow,
										EventType = MantaEventType.Abuse,
										SendID = (snd == null ? string.Empty : snd.ID)
									});
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

		/// <summary>
		/// Saves a Manta Event.
		/// </summary>
		/// <param name="evt">Event to save.</param>
		/// <returns>The Events ID</returns>
		internal int Save(MantaEvent evt)
		{
			evt.ID = MantaMTA.Core.DAL.EventDB.Save(evt);
			if (evt is MantaBounceEvent)
				MantaMTA.Core.DAL.EventDB.Save(evt as MantaBounceEvent);
			return evt.ID;
		}

		/// <summary>
		/// Gets a Manta Event.
		/// </summary>
		/// <param name="ID">ID of the Event to get.</param>
		/// <returns>The MantaEvent or NULL if ID doesn't belong to any.</returns>
		internal MantaEvent GetEvent(int ID)
		{
			return MantaMTA.Core.DAL.EventDB.GetEvent(ID);
		}

		/// <summary>
		/// Gets all of the MantaEvents that Manta knows about.
		/// </summary>
		/// <returns>Collection of MantaEvent objects.</returns>
		internal MantaEventCollection GetEvents()
		{
			return MantaMTA.Core.DAL.EventDB.GetEvents();
		}
	}
}
