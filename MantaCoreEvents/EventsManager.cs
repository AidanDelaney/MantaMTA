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


			MessageHeader returnPath = msg.Headers.GetFirst("Return-Path");
			string rcptTo = string.Empty;
			int internalSendID = 0;

			if (!ReturnPathManager.TryDecode(returnPath.Value, out rcptTo, out internalSendID))
			{
				// Not a valid Return-Path so can't process.
				return EmailProcessingResult.SuccessNoAction;
			}






			MantaBounceEvent bounceEvent = new MantaBounceEvent();
			bounceEvent.EmailAddress = rcptTo;
			bounceEvent.SendID = DAL.SendIdDb.GetSendIdFromInternalSendId(internalSendID);

			// Might be good to get the DateTime found in the email at a later point.
			bounceEvent.EventTime = DateTime.UtcNow;

			// These properties are both down to what SMTP code we find, if any.
			bounceEvent.BounceCode = MantaBounceCode.Unknown;
			bounceEvent.BounceType = MantaBounceType.Unknown;



			// First, try to find a NonDeliveryReport body part as that's the proper way for an MTA
			// to tell us there was an issue sending the email.
			IEnumerable<MimeMessageBodyPart> ndrBodies = msg.BodyParts.Where(b => b.ContentType.MediaType.Equals("message/delivery-status", StringComparison.OrdinalIgnoreCase));

			foreach(MimeMessageBodyPart b in ndrBodies)
			{
				MantaBounceType bType = MantaBounceType.Unknown;
				MantaBounceCode bCode = MantaBounceCode.Unknown;
				string bMsg = string.Empty;


				// Successfully parsed?
				if (ParseNdr(b.GetDecodedBody(), out bType, out bCode, out bMsg))
				{
					bounceEvent.BounceType = bType;
					bounceEvent.BounceCode = bCode;
					bounceEvent.Message = bMsg;

					// Write BounceEvent to DB.
					// TODO

					return EmailProcessingResult.SuccessBounce;
				}
			}


			// No NDR part, have to to this the manual way and check all content.
			foreach (MimeMessageBodyPart b in msg.BodyParts)
			{
				MantaBounceType bType = MantaBounceType.Unknown;
				MantaBounceCode bCode = MantaBounceCode.Unknown;
				string bMsg = string.Empty;

				if (ParseBounceMessage(b.GetDecodedBody(), out bType, out bCode, out bMsg))
				{
					bounceEvent.BounceType = bType;
					bounceEvent.BounceCode = bCode;
					bounceEvent.Message = bMsg;

					// Write BounceEvent to DB.
					// TODO

					return EmailProcessingResult.SuccessBounce;
				}
			}
			
			
			// return ProcessBounce(from, to, message);
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
		internal bool ParseNdr(string message, out MantaBounceType bounceType, out MantaBounceCode bounceCode, out string bounceMessage)
		{
			// Check for the Diagnostic-Code as hopefully contains more information about the error.
			Match m = Regex.Match(message, @"^Diagnostic\-Code\:\s+(?<Code>.*)$", RegexOptions.Multiline);
			if (m != null)
			{
				if (ParseBounceMessage(m.Groups["Code"].Value, out bounceType, out bounceCode, out bounceMessage))
				{

				}

			}
			else
			{
				// Looks like all we've got is the Status Code.
				m = Regex.Match(message, @"^Status\:\s+(?<Status>.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

				if (m != null)
				{
					if (m.Groups["Status"].Value.StartsWith("5"))
					{
						// 5xx codes are permanent errors.
						bounceType = MantaBounceType.Hard;
						bounceCode = MantaBounceCode.BounceUnknown;
					}
					else if (m.Groups["Status"].Value.StartsWith("4"))
					{
						// 4xx codes are temporary errors.
						bounceType = MantaBounceType.Soft;
						bounceCode = MantaBounceCode.BounceUnknown;
					}
					else
					{
						bounceType = MantaBounceType.Unknown;
						bounceCode = MantaBounceCode.BounceUnknown;
					}

					// Use the whole message.
					bounceMessage = message;
					return true;
				}
			}


			bounceType = MantaBounceType.Unknown;
			bounceCode = MantaBounceCode.Unknown;
			bounceMessage = string.Empty;
			return false;
		}


		/// <summary>
		/// Examines an SMTP response message to identify detailed bounce information from it.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="rcptTo"></param>
		/// <param name="internalSendID"></param>
		/// <returns>A MantaBounceEvent object with details of the bounce.</returns>
		private MantaBounceEvent ProcessSmtpResponseMessage(string message, string rcptTo, int internalSendID)
		{
			MantaBounceEvent bounceEvent = new MantaBounceEvent();
			bounceEvent.EventType = MantaEventType.Unknown;
			bounceEvent.EmailAddress = rcptTo;
			bounceEvent.SendID = DAL.SendIdDb.GetSendIdFromInternalSendId(internalSendID);

			// It is possible that the bounce was generated a while back, but we're assuming "now" for the moment.
			// Might be good to get the DateTime found in the email at a later point.
			bounceEvent.EventTime = DateTime.UtcNow;


			MantaBounceCode bCode = MantaBounceCode.Unknown;
			MantaBounceType bType = MantaBounceType.Unknown;
			string bounceMessage = string.Empty;

			if (ParseBounceMessage(message, out bType, out bCode, out bounceMessage))
			{
				bounceEvent.EventType = MantaEventType.Bounce;
				bounceEvent.BounceCode = bCode;
				bounceEvent.BounceType = bType;
				bounceEvent.Message = bounceMessage;
			}
			else
			{
				bounceEvent.BounceCode = MantaBounceCode.Unknown;
				bounceEvent.BounceType = MantaBounceType.Unknown;
				bounceEvent.Message = string.Empty;
			}



			return bounceEvent;
		}


		/// <summary>
		/// Runs Bounce Rules against a message.
		/// </summary>
		/// <param name="message">Could either be an email body part or a single or multiple line response from another MTA.</param>
		/// <param name="bounceType">out.</param>
		/// <param name="bounceCode">out.</param>
		/// <param name="bounceMessage">out.</param>
		/// <returns>true if a Bounce Rule matched the <paramref name="message"/>, else false.</returns>
		internal bool ParseBounceMessage(string message, out MantaBounceType bounceType, out MantaBounceCode bounceCode, out string bounceMessage)
		{
			foreach (BounceRule r in BounceRulesManager.BounceRules)
			{
				// If we get a match, we're done processing Rules.
				if (r.IsMatch(message, out bounceMessage))
				{
					bounceType = MantaBounceType.Unknown;
					bounceCode = MantaBounceCode.Unknown;
					return true;
				}
			}

			// No Rules matched.
			bounceType = MantaBounceType.Unknown;
			bounceCode = MantaBounceCode.Unknown;
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
