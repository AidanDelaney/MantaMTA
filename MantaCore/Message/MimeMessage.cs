using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MantaMTA.Core.Message
{
	/// <summary>
	/// Mime message class to help handle messages that have been received.
	/// Used by MantaMTA.Core.Events and will be used to display abuse/postmaster@ emails in web interface.
	/// </summary>
	public class MimeMessage
	{
		/// <summary>
		/// Collection of the Mime message headers.
		/// </summary>
		public MessageHeaderCollection Headers { get; set; }

		/// <summary>
		/// Collection of the mime messages body parts.
		/// </summary>
		public MimeMessageBodyPart[] BodyParts { get; set; }


		/// <summary>
		/// Create a MIME Message from <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message to parse.</param>
		/// <returns>A MimeMessage object if successful otherwise NULL</returns>
		public static MimeMessage Parse(string message)
		{
			try
			{
				MimeMessage mimeMessage = new MimeMessage();

				// Get the message headers.
				mimeMessage.Headers = MessageManager.GetMessageHeaders(message);

				// Get the message content type.
				MessageHeader contentType = mimeMessage.Headers.SingleOrDefault(h => h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

				// Make sure the "Content-Type" header exists.
				if (contentType == null)
				{
					Logging.Debug("No content type header");
					return null;
				}

				// If the message isn't multipart then it isn't a Mime message.
				if (!contentType.Value.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
				{
					Logging.Debug("Not a MIME Message");
					return null;
				}

				// Get the body parts.
				mimeMessage.BodyParts = GetBodyParts(MessageManager.GetMessageBodySection(message));

				// Return it.
				return mimeMessage;
			}
			catch (Exception ex)
			{
				Logging.Debug("Failed to parse to MimeMessage " + Environment.NewLine + message, ex);
				// MIME message could contain anything so return null on exception.
				return null;
			}
		}

		/// <summary>
		/// Extracts the body parts from <paramref name="bodyContent"/>
		/// </summary>
		/// <param name="bodyContent">The body content of the MIME Messsage that contains boundries.</param>
		/// <returns>Collection of Body Parts.</returns>
		private static MimeMessageBodyPart[] GetBodyParts(string bodyContent)
		{
			// The collection we will return.
			List<MimeMessageBodyPart> bodyParts = new List<MimeMessageBodyPart>();

			using (StringReader reader = new StringReader(bodyContent))
			{
				// This will be set to true when we are looking at a boundry line (may be folded). Don't wan't to unfold as it
				// will take quite a while.
				bool loopInBoundryMarker = false;

				// This is used to hold the current body part as we step through the message.
				MimeMessageBodyPart currentBodyPart = null;

				// As long as there are lines keep looping.
				while (reader.Peek() > -1)
				{
					// Get the next line for us to work with.
					string line = reader.ReadLine();

					// If we are in a boundry marker check that we haven't exited it.
					if (loopInBoundryMarker)
					{
						// Blank line signifies end of boundry marker.
						if (string.IsNullOrEmpty(line))
						{
							loopInBoundryMarker = false;

							// Read the next line as we are now out of the boundry marker.
							line = reader.ReadLine();
						}
					}
					else
					{
						// If we aren't in a boundry marker look to see if we have entered one.
						if (line.StartsWith("--") && !line.EndsWith("--"))
						{
							// Get the current boundry name or use -- as that will match all bounderies.
							string boundry = currentBodyPart != null ? currentBodyPart.Boundry : "--";

							// Make sure the boundry is for this MIME message and not a child message.
							if (line.Contains(boundry))
							{
								currentBodyPart = new MimeMessageBodyPart();
								currentBodyPart.Boundry = line;
								bodyParts.Add(currentBodyPart);
								loopInBoundryMarker = true;
							}
						}
						// We have reached the end of the message.
						else if (line.StartsWith("--") && line.EndsWith("--"))
							break;
					}

					// If we are in a boundry marker then check for conent- properties.
					if (loopInBoundryMarker)
					{
						// Look for the Content-Transfer-Encoding.
						Match m = Regex.Match(line, @"Content-Transfer-Encoding:\s+(?<TransferEncoding>\S+)", RegexOptions.IgnoreCase);

						if (m.Success)
						{
							// Found the transfer encoding. Set it in the body marker.
							switch (m.Groups["TransferEncoding"].Value.ToUpper())
							{
								case "BASE64":
									currentBodyPart.TransferEncoding = TransferEncoding.Base64;
									break;
								case "8BIT":
									currentBodyPart.TransferEncoding = TransferEncoding.EightBit;
									break;
								case "QUOTED-PRINTABLE":
									currentBodyPart.TransferEncoding = TransferEncoding.QuotedPrintable;
									break;
								case "7BIT":
									currentBodyPart.TransferEncoding = TransferEncoding.SevenBit;
									break;
								default:
									currentBodyPart.TransferEncoding = TransferEncoding.Unknown;
									break;
							}
						}

						// Look for the content type.
						m = Regex.Match(line, @"Content-Type:\s+(?<ContentType>\S+)", RegexOptions.IgnoreCase);
						if (m.Success)
							currentBodyPart.ContentType = new System.Net.Mime.ContentType(m.Groups["ContentType"].Value);
					}
					// Not in a boundry marker so must be body part content.
					else if (currentBodyPart != null)
						currentBodyPart.EncodedBody += line + Environment.NewLine;
				}
			}

			return bodyParts.ToArray();
		}
	}
}
