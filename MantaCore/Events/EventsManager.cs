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
		/// Examines an email to try to identify detailed bounce information from it.
		/// </summary>
		/// <param name="filename">Path and filename of the file being processed.</param>
		/// <param name="message">The entire text content for an email (headers and body).</param>
		/// <returns>An EmailProcessingResult value indicating whether the the email was
		/// successfully processed or not.</returns>
		public EmailProcessingDetails ProcessBounceEmail(string message)
		{
			EmailProcessingDetails bounceIdDetails = new EmailProcessingDetails();

			MimeMessage msg = MimeMessage.Parse(message);

			if (msg == null)
			{
				bounceIdDetails.ProcessingResult = EmailProcessingResult.ErrorContent;
				bounceIdDetails.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;
				return bounceIdDetails;
			}


			// "x-receiver" should contain what Manta originally set as the "return-path" when sending.
			MessageHeader returnPath = msg.Headers.GetFirstOrDefault("x-receiver");
			if (returnPath == null)
			{
				bounceIdDetails.ProcessingResult = EmailProcessingResult.ErrorNoReturnPath;
				bounceIdDetails.BounceIdentifier = Core.Enums.BounceIdentifier.UnknownReturnPath;
				return bounceIdDetails;
			}

			string rcptTo = string.Empty;
			int internalSendID = 0;

			if (!ReturnPathManager.TryDecode(returnPath.Value, out rcptTo, out internalSendID))
			{
				// Not a valid Return-Path so can't process.
				bounceIdDetails.ProcessingResult = EmailProcessingResult.ErrorNoReturnPath;
				bounceIdDetails.BounceIdentifier = Core.Enums.BounceIdentifier.UnknownReturnPath;
				return bounceIdDetails;
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
			BodyPart deliveryReportBodyPart;
			string deliveryReport = string.Empty;

			if (FindFirstBodyPartByMediaType(msg.BodyParts, "message/delivery-status", out deliveryReportBodyPart))
			{
				// If we've got a delivery report, check it for info.

				// Abuse report content may have long lines whitespace folded.
				deliveryReport = MimeMessage.UnfoldHeaders(deliveryReportBodyPart.GetDecodedBody());
			
				if (ParseNdr(deliveryReport, out bouncePair, out bounceMsg, out bounceIdDetails))
				{
					// Successfully parsed.
					bounceEvent.BounceInfo = bouncePair;
					bounceEvent.Message = bounceMsg;

					// Write BounceEvent to DB.
					Save(bounceEvent);

					bounceIdDetails.ProcessingResult = EmailProcessingResult.SuccessBounce;
					return bounceIdDetails;
				}
			}



			// We're still here so there was either no NDR part or nothing contained within it that we could
			// interpret so have to check _all_ body parts for something useful.
			if (FindBounceReason(msg.BodyParts, out bouncePair, out bounceMsg, out bounceIdDetails))
			{
				bounceEvent.BounceInfo = bouncePair;
				bounceEvent.Message = bounceMsg;

				// Write BounceEvent to DB.
				Save(bounceEvent);

				bounceIdDetails.ProcessingResult = EmailProcessingResult.SuccessBounce;
				return bounceIdDetails;
			}





			// Nope - no clues relating to why the bounce occurred.
			bounceEvent.BounceInfo.BounceType = MantaBounceType.Unknown;
			bounceEvent.BounceInfo.BounceCode = MantaBounceCode.Unknown;
			bounceEvent.Message = string.Empty;
			
			bounceIdDetails.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;
			bounceIdDetails.ProcessingResult = EmailProcessingResult.Unknown;
			return bounceIdDetails;
		}


		/// <summary>
		/// Examines a non-delivery report for detailed bounce information.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="bounceType"></param>
		/// <param name="bounceCode"></param>
		/// <param name="bounceMessage"></param>
		/// <returns></returns>
		internal bool ParseNdr(string message, out BouncePair bouncePair, out string bounceMessage, out EmailProcessingDetails bounceIdentification)
		{
			bounceIdentification = new EmailProcessingDetails();


			// Check for the Diagnostic-Code as hopefully contains more information about the error.
			const string DiagnosticCodeFieldName = "Diagnostic-Code: ";
			const string StatusFieldName = "Status: ";



			StringBuilder diagnosticCode = new StringBuilder(string.Empty);
			string status = string.Empty;

			using (StringReader sr = new StringReader(message))
			{
				string line = sr.ReadToCrLf();

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
							line = sr.ReadToCrLf();

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
							line = sr.ReadToCrLf();
						else
							line = string.Empty;
					}
				}
			}


			// Process what we've managed to find...

			// Diagnostic-Code
			if (!string.IsNullOrWhiteSpace(diagnosticCode.ToString()))
			{
				if (ParseSmtpDiagnosticCode(diagnosticCode.ToString(), out bouncePair, out bounceMessage, out bounceIdentification))
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

					bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NdrCode;
					bounceIdentification.MatchingValue = m.Value;
					return true;
				}
			}




			// If we've not already returned from this method, then we're still looking for an explanation
			// for the bounce so parse the entire message as a string.
			if (ParseBounceMessage(message, out bouncePair, out bounceMessage, out bounceIdentification))
				return true;
			


			// Nope - no clues relating to why the bounce occurred.
			bouncePair.BounceType = MantaBounceType.Unknown;
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bounceMessage = string.Empty;

			bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;
			bounceIdentification.MatchingValue = string.Empty;

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
		internal bool ParseSmtpDiagnosticCode(string message, out BouncePair bouncePair, out string bounceMessage, out EmailProcessingDetails bounceIdentification)
		{
			// Remove "smtp;[possible whitespace]" if it appears at the beginning of the message.
			if (message.StartsWith("smtp;", StringComparison.OrdinalIgnoreCase))
				message = message.Substring("smtp;".Length).Trim();

			if (ParseBounceMessage(message, out bouncePair, out bounceMessage, out bounceIdentification))
				return true;


			bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;

			return false;
		}


		/// <summary>
		/// Examines an SMTP response message to identify detailed bounce information from it.
		/// </summary>
		/// <param name="response">The message that's come back from an external MTA when attempting to send an email.</param>
		/// <param name="rcptTo">The email address that was being sent to.</param>
		/// <param name="internalSendID">The internal Manta SendID.</param>
		/// <returns>True if a bounce was found and recorded, false if not.</returns>
		internal bool ProcessSmtpResponseMessage(string response, string rcptTo, int internalSendID, out EmailProcessingDetails bounceIdentification)
		{
			bounceIdentification = new EmailProcessingDetails();

			BouncePair bouncePair = new BouncePair();
			string bounceMessage = string.Empty;

			if (ParseBounceMessage(response, out bouncePair, out bounceMessage, out bounceIdentification))
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

			bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;

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
		internal bool ParseBounceMessage(string message, out BouncePair bouncePair, out string bounceMessage, out EmailProcessingDetails bounceIdentification)
		{
			bounceIdentification = new EmailProcessingDetails();



			// Check all Bounce Rules for a match.
			foreach (BounceRule r in BounceRulesManager.BounceRules)
			{
				// If we get a match, we're done processing Rules.
				if (r.IsMatch(message, out bounceMessage))
				{
					bouncePair.BounceType = r.BounceTypeIndicated;
					bouncePair.BounceCode = r.BounceCodeIndicated;
					bounceMessage = message;

					bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.BounceRule;
					bounceIdentification.MatchingBounceRuleID = r.RuleID;
					bounceIdentification.MatchingValue = r.Criteria;

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
					if (bouncePair.BounceType != MantaBounceType.Unknown)
					{
						bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NdrCode;
						bounceIdentification.MatchingValue = match.Groups["NdrCode"].Value;

						return true;
					}
				}

				// Try the SMTP code as there wasn't an NDR.
				if (match.Groups["SmtpCode"].Success && match.Groups["SmtpCode"].Length > 0)
				{
					bouncePair = BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(Int32.Parse(match.Groups["SmtpCode"].Value));

					bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.SmtpCode;
					bounceIdentification.MatchingValue = match.Groups["SmtpCode"].Value;

					return true;
				}
			}


			// Failed to identify a reason so shouldn't be a bounce.
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bouncePair.BounceType = MantaBounceType.Unknown;
			bounceMessage = string.Empty;

			bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;
			bounceIdentification.MatchingValue = string.Empty;

			return false;
		}


		/// <summary>
		// Attempts to find the first body part with the specified Media Type from a collection of BodyParts.
		/// </summary>
		/// <param name="bodyParts">An array of BodyParts to search within (including any child BodyParts).</param>
		/// <param name="mediaTypeToFind">The media type of the BodyPart to find, e.g. "message/delivery-status"
		/// or "message/feedback-report".</param>
		/// <param name="report">out.  If a BodyPart with a MediaType matching the value in <paramref name="mediaTypeToFind"/> is found,
		/// this will contain the content of it, else string.Empty.</param>
		/// <returns>true if a delivery report was found, else false.</returns>
		internal bool FindFirstBodyPartByMediaType(BodyPart[] bodyParts, string mediaTypeToFind, out BodyPart foundBodyPart)
		{
			foreach (BodyPart bp in bodyParts)
			{
				if (bp.ContentType.MediaType.Equals(mediaTypeToFind, StringComparison.OrdinalIgnoreCase))
				{
					// Found it!
					foundBodyPart = bp;
					return true;
				}
				else if (bp.ContentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
				{
					// Loop through the child body parts.
					if (FindFirstBodyPartByMediaType(bp.BodyParts, mediaTypeToFind, out foundBodyPart))
						return true;
				}
				else
					// Ignore this BodyPart as it's not a container or the media type we're looking for.
					continue;
			}


			// If we're still here, then we didn't find a bodypart with the media type we're looking for.
			foundBodyPart = null;
			return false;
		}


		/// <summary>
		/// Attempts to find and process the reason an email bounced by digging through all body parts.
		/// </summary>
		/// <param name="bodyParts">Array of BodyParts to dig through; may include child body parts.</param>
		/// <param name="bouncePair">out.  A BouncePair object containing details of the bounce (if a reason was found).</param>
		/// <param name="bounceMessage">out.  The text to use as the reason found why the bounce occurred.</param>
		/// <returns>true if a reason was found, else false.</returns>
		internal bool FindBounceReason(BodyPart[] bodyParts, out BouncePair bouncePair, out string bounceMessage, out EmailProcessingDetails bounceIdentification)
		{
			foreach(BodyPart b in bodyParts)
			{
				if (b.ContentType.MediaType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
				{
					// Just a container for other body parts so check them.
					if (FindBounceReason(b.BodyParts, out bouncePair, out bounceMessage, out bounceIdentification))
						return true;
				}
				else if (b.ContentType.MediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
				{
					// Only useful to examine text/plain body parts (so not "image/gif" etc).
					if (ParseBounceMessage(b.GetDecodedBody(), out bouncePair, out bounceMessage, out bounceIdentification))
						return true;
				}
				// else
				//	Console.WriteLine("\tSkipped bodypart \"" + b.ContentType.MediaType + "\".");
			}


			// Still here so haven't found anything useful.
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bouncePair.BounceType = MantaBounceType.Unknown;
			bounceMessage = string.Empty;

			bounceIdentification = new EmailProcessingDetails();
			bounceIdentification.BounceIdentifier = Core.Enums.BounceIdentifier.NotIdentifiedAsABounce;

			return false;
		}


		/// <summary>
		/// Looks through a feedback loop email looking for something to identify it as an abuse report and who it relates to.
		/// If found, logs the event.
		/// 
		/// How to get the info depending on the ESP (and this is likely to be the best order to check for each too):
		/// Abuse Report Original-Mail-From.										[Yahoo]
		/// Message-ID from body part child with content-type of message/rfc822.	[AOL]
		/// Return-Path in main message headers.									[Hotmail]
		/// </summary>
		/// <param name="message">The feedback look email.</param>
		public EmailProcessingDetails ProcessFeedbackLoop(string content)
		{
			EmailProcessingDetails processingDetails = new EmailProcessingDetails();


			MimeMessage message = MimeMessage.Parse(content);
			if (message == null)
			{
				processingDetails.ProcessingResult = EmailProcessingResult.ErrorContent;
				return processingDetails;
			}

			try
			{
				// Step 1: Yahoo! provide useable Abuse Reports (AOL's are all redacted).
				//Look for abuse report
				BodyPart abuseBodyPart = null;
				string abuseReportBody;

				if (FindFirstBodyPartByMediaType(message.BodyParts, "message/feedback-report", out abuseBodyPart))
				{
					// Found an abuse report body part to examine.

					// Abuse report content may have long lines whitespace folded.
					abuseReportBody = MimeMessage.UnfoldHeaders(abuseBodyPart.GetDecodedBody());

					using (StringReader reader = new StringReader(abuseReportBody))
					{
						while (reader.Peek() > -1)
						{
							string line = reader.ReadToCrLf();

							// The original mail from value will be the return-path we'd set so we should be able to get all the values we need from that.
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

										processingDetails.ProcessingResult = EmailProcessingResult.SuccessAbuse;
										return processingDetails;
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



				
				// Function to use against BodyParts to find a return-path header.
				Func<MessageHeaderCollection, bool> checkForReturnPathHeaders = new Func<MessageHeaderCollection, bool>(delegate(MessageHeaderCollection headers)
					{
						MessageHeader returnPathHeader = headers.GetFirstOrDefault("Return-Path");
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

						MessageHeader messageIdHeader = headers.GetFirstOrDefault("Message-ID");
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
					}
				);


				// Step 2: AOL give redacted Abuse Reports but include the original email as a bodypart; find that.
				BodyPart childMessageBodyPart;
				if (FindFirstBodyPartByMediaType(message.BodyParts, "message/rfc822", out childMessageBodyPart))
				{
					if (checkForReturnPathHeaders(childMessageBodyPart.Headers))
					{
						processingDetails.ProcessingResult = EmailProcessingResult.SuccessAbuse;
						return processingDetails;
					}
				}


				// Step 3: Hotmail don't do Abuse Reports, they just return our email to us exactly as we sent it.
				if (checkForReturnPathHeaders(message.Headers))
				{
					processingDetails.ProcessingResult = EmailProcessingResult.SuccessAbuse;
					return processingDetails;
				}
			}
			catch (Exception) { }

			Logging.Debug("Failed to find return path!");

			processingDetails.ProcessingResult = EmailProcessingResult.ErrorNoReturnPath;
			return processingDetails;
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
