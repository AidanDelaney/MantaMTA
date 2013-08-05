using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MantaMTA.Core.Message;
using System.Text.RegularExpressions;

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
			internal static string SmtpResponse = @"^.*?([\s\b]*?(((?<SmtpCode>\d{3})(?:[^\d-]*?))|(?<NdrCode>\d{1}\.\d{1,3}\.\d{1,3}))[\s\b])+(?<Detail>.*)$";

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
			MessageHeader returnPath = msg.Headers.GetFirst("x-receiver");
			string rcptTo = string.Empty;
			int internalSendID = 0;

			if (!ReturnPathManager.TryDecode(returnPath.Value, out rcptTo, out internalSendID))
			{
				// Not a valid Return-Path so can't process.
				return EmailProcessingResult.SuccessNoAction;
			}






			MantaBounceEvent bounceEvent = new MantaBounceEvent();
			bounceEvent.EmailAddress = rcptTo;
			bounceEvent.SendID = MantaMTA.Core.DAL.SendDb.GetSendIdFromInternalSendId(internalSendID);

			// TODO: Might be good to get the DateTime found in the email.
			bounceEvent.EventTime = DateTime.UtcNow;

			// These properties are both set according to the SMTP code we find, if any.
			bounceEvent.BounceInfo.BounceCode = MantaBounceCode.Unknown;
			bounceEvent.BounceInfo.BounceType = MantaBounceType.Unknown;



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
					// TODO

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



			string[] lines = message.Split(new string[] { MtaParameters.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			string diagnosticCode = string.Empty;
			string status = string.Empty;
			int lineIndex = 0;


			// Go through the message line by line.
			while(lineIndex < lines.Length)
			{
				string l = lines[lineIndex];


				// Skip blank lines.
				if (string.IsNullOrWhiteSpace(l))
				{
					lineIndex++;
					continue;
				}



				// Check if the line begins with the name of a field we can examine.

				if (l.StartsWith(DiagnosticCodeFieldName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(diagnosticCode))
				{
					// "Diagnostic-Code" Field.  Preferred to "Status" Field as it contains more detail.

					// Get the value and remove any surrounding whitespace.
					diagnosticCode = l.Substring(DiagnosticCodeFieldName.Length).Trim();

					
					// Advance to the next line to check for more folded content.
					// If subsequent lines are prefixed by whitespace, then this field has more content.
					lineIndex++;

					while (lineIndex < lines.Length)
					{				

						l = lines[lineIndex];


						if (!string.IsNullOrWhiteSpace(l) && ((l.StartsWith(" ") || l.StartsWith("\t"))))
						{
							// There's more...
							diagnosticCode += l.Trim();
						}
						else
							break;

						lineIndex++;
					}
					
				}
				else if (l.StartsWith(StatusFieldName, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(status))
				{
					// "Status" Field.  If no "Diagnostic-Code", this is the second best thing to check.

					// Get the value and remove any surrounding whitespace.
					status = l.Substring(StatusFieldName.Length).Trim();
				}


				// Have we got all we need?
				if (!string.IsNullOrWhiteSpace(diagnosticCode) && !string.IsNullOrWhiteSpace(status))
					break;


				lineIndex++;
			}



			// Process what we've managed to find...

			// Diagnostic-Code
			if (!string.IsNullOrWhiteSpace(diagnosticCode))
			{
				if (ParseSmtpDiagnosticCode(diagnosticCode, out bouncePair, out bounceMessage))
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
		/// <param name="message">The message that's come back from an external MTA when attempting to send an email.</param>
		/// <param name="rcptTo">The email address that was being sent to.</param>
		/// <param name="internalSendID">The internal Manta SendID.</param>
		/// <returns>A MantaBounceEvent object with details of the bounce.</returns>
		internal MantaBounceEvent ProcessSmtpResponseMessage(string message, string rcptTo, int internalSendID)
		{
			MantaBounceEvent bounceEvent = new MantaBounceEvent();
			bounceEvent.EventType = MantaEventType.Unknown;
			bounceEvent.EmailAddress = rcptTo;
			bounceEvent.SendID = MantaMTA.Core.DAL.SendDb.GetSendIdFromInternalSendId(internalSendID);

			// It is possible that the bounce was generated a while back, but we're assuming "now" for the moment.
			// Might be good to get the DateTime found in the email at a later point.
			bounceEvent.EventTime = DateTime.UtcNow;


			BouncePair bouncePair = new BouncePair();
			string bounceMessage = string.Empty;

			if (ParseBounceMessage(message, out bouncePair, out bounceMessage))
			{
				// Got some information about the bounce.
				bounceEvent.EventType = MantaEventType.Bounce;
				bounceEvent.BounceInfo = bouncePair;
				bounceEvent.Message = bounceMessage;
			}
			else
			{
				// Wasn't able to identify if it was a bounce.
				bounceEvent.EventType = MantaEventType.Unknown;
				bounceEvent.BounceInfo.BounceType = MantaBounceType.Unknown;
				bounceEvent.BounceInfo.BounceCode = MantaBounceCode.Unknown;
				bounceEvent.Message = string.Empty;
			}


			return bounceEvent;
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
					return true;
				}
			}


			// No Bounce Rules match the message so try to get a match on an NDR code ("5.1.1") or an SMTP code ("550").
			// TODO: Handle several matches being found - somehow find The Best?
			// Pattern: Should match like this:
			//	[anything at the beginning if present][then either an SMTP code or an NDR code, but both should be grabbed if
			// they exist][then the rest of the content (if any)]
			Match match = Regex.Match(message, RegexPatterns.SmtpResponse, RegexOptions.Multiline | RegexOptions.IgnoreCase);

			if (match.Success)
			{
				// Check for anything useful with the NDR code first as it contains more specific detail than the SMTP code.
				if (match.Groups["NdrCode"].Success)
					bouncePair = BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair(match.Groups["NdrCode"].Value);
				// Try the SMTP code as there wasn't an NDR.
				else if (match.Groups["SmtpCode"].Success)
					bouncePair = BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(Int32.Parse(match.Groups["SmtpCode"].Value));
				else
					// If we're here, then the Regex pattern shouldn't really have matched as it specifies that an NDR
					// and/or an SMTP code must appear, but neither have.  Check the pattern.
					throw new Exception("Unable to process bounce: NDR and/or SMTP codes indicated but neither found.");


				bounceMessage = match.Value.Trim();

				return true;
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
				if (bp.ContentType.MediaType == "message/delivery-status")
				{
					// Found it!
					report = bp.GetDecodedBody();
					return true;
				}
				else if (bp.ContentType.MediaType.StartsWith("multipart/"))
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
				if (b.ContentType.MediaType.StartsWith("multipart/"))
				{
					// Just a container for other body parts so check them.
					if (FindBounceReason(b.BodyParts, out bouncePair, out bounceMessage))
						return true;
				}
				else
				{
					if (ParseBounceMessage(b.GetDecodedBody(), out bouncePair, out bounceMessage))
						return true;
				}
			}


			// Still here so haven't found anything useful.
			bouncePair.BounceCode = MantaBounceCode.Unknown;
			bouncePair.BounceType = MantaBounceType.Unknown;
			bounceMessage = string.Empty;
			return false;
		}



		/// <summary>
		/// Examines a string to identify detailed bounce information from it.
		/// </summary>
		/// <param name="emailContent"></param>
		public EmailProcessingResult ProcessFeedbackLoop(string message)
		{
			/*
			 
			abus@manta.io
			
			 
			*/


			// Check the values found in the report.  Things like OriginalSender and RecipientEmail.


			// Check return-path indicates it's for us.

			return EmailProcessingResult.Unknown;
		}
	}
}
