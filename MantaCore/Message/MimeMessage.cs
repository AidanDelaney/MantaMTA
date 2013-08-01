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


		public static MimeMessage Parse2(string message)
		{
			MimeMessage msg = new MimeMessage();
			string headersChunk;
			string bodyChunk;
			SeparateBodyPartHeadersAndBody(message, out headersChunk, out bodyChunk);

			msg.Headers = GetMessageHeaders(headersChunk);

			// Check the email is a MIME message.
			if (msg.Headers.GetFirst("MIME-Version") == null)
			{
				// TODO BenC (2013-07-25): Think this just means there's a single, plain text body part
				// so we add code here to handle it.

				Logging.Debug("Not a MIME Message");
				return null;
			}


			// Get the boundary of the message and use that to chop up the content, then loop through each
			// of those chunks looking for bodyparts within each.
			ContentType msgContentType = new ContentType(msg.Headers.GetFirst("Content-Type").Value);

			// Content-Type: multipart/alternative; differences=Content-Type;
			//  boundary="b2420641-bd9f-4b3d-9a4f-c14c58c6ba30"
			// msgContentType.Value.Contains("boundary")
			



			
			msg.BodyParts = DivideIntoBodyParts(bodyChunk, msgContentType.Boundary);

			return msg;
		}


		private static MimeMessageBodyPart[] DivideIntoBodyParts(string content, string boundary)
		{
			List<MimeMessageBodyPart> bodyParts = new List<MimeMessageBodyPart>();

			// Get from "--[boundary]" to "--[boundary]".

			int bodyStart = content.IndexOf("--" + boundary);
			if (bodyStart == -1)
			{
				// Didn't find the boundary - very odd.
				Logging.Debug("Boundary not found in body part.");
				return null;
			}

			content = content.Substring(bodyStart);


			string[] parts = content.Split(new string[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < parts.Length; i++)
			{
				string p = parts[i];

				// Ignore empty strings and "--" (the latter will come from Split()ing on the boundary and the final boundary
				// is in the format "--[boundary]--" so there'll be one of just "--[crlf][crlf]" at the end).
				if (string.IsNullOrWhiteSpace(p) || p.Trim() == "--")
					continue;


				// Remove the initial carriage return from the beginning of a body part's content (will appear
				// immediatley after the boundary).
				p = p.StartsWith(MtaParameters.NewLine) ? p.Substring(MtaParameters.NewLine.Length) : p;



				// Got an actual body part.
				MimeMessageBodyPart bp = CreateBodyPart(p);


				bodyParts.Add(bp);
			}



			return bodyParts.ToArray();
		}


		/// <summary>
		/// Converts a string of MIME content into a MimeMessageBodyPart object.
		/// </summary>
		/// <param name="content"></param>
		/// <returns>A MimeMessageBodyPart represented by <paramref name="content"/>.</returns>
		private static MimeMessageBodyPart CreateBodyPart(string content)
		{

			MimeMessageBodyPart bp = new MimeMessageBodyPart();
			string headersChunk;
			string bodyChunk;

			// Split "headers" and "body".
			SeparateBodyPartHeadersAndBody(content, out headersChunk, out bodyChunk);

			MessageHeaderCollection headers = GetMessageHeaders(headersChunk);


			// Get the key values from the headers that indicate how to handle this BodyPart.
			MessageHeader temp = headers.GetFirst("Content-Type");
			if (temp != null)
			{
				bp.ContentType = new ContentType(temp.Value);
				// Content-Type: multipart/alternative; differences=Content-Type;
				//  boundary="b2420641-bd9f-4b3d-9a4f-c14c58c6ba30"
			}
			else
				// Default is specified in RFCs as "text/plain; charset=us-ascii".
				bp.ContentType = new ContentType("text/plain; charset=us-ascii");


			temp = headers.GetFirst("Content-Transfer-Encoding");
			if (temp != null)
				bp.TransferEncoding = IdentifyTransferEncoding(temp.Value);
			else
				// Default is specified in RFCs as "7bit".
				bp.TransferEncoding = TransferEncoding.SevenBit;


			if (bp.ContentType.MediaType.StartsWith("multipart/"))
			{
				// This bodypart is just a container for more bodyparts.
				bp.BodyParts = DivideIntoBodyParts(bodyChunk, bp.ContentType.Boundary);
			}
			else
			{
				bp.EncodedBody = bodyChunk;
			}


			return bp;
		}


		private static TransferEncoding IdentifyTransferEncoding(string transferEncoding)
		{
			switch (transferEncoding.ToUpper())
			{
				case "BASE64":
					return TransferEncoding.Base64;
				// BenC (2013-07-05): .NET 4.0 doesn't have EightBit.
				//case "8BIT":
				//    return TransferEncoding.EightBit;
				case "QUOTED-PRINTABLE":
					return TransferEncoding.QuotedPrintable;
				case "7BIT":
					return TransferEncoding.SevenBit;
				default:
					return TransferEncoding.Unknown;
			}
		}


		/// <summary>
		/// Gets a collection of the message headers, either from an email or a MIME body part.
		/// </summary>
		/// <param name="messageData">The raw message data.</param>
		/// <returns>Collection of headers.</returns>
		public static MessageHeaderCollection GetMessageHeaders(string headersChunk)
		{
			if (string.IsNullOrWhiteSpace(headersChunk))
				return new MessageHeaderCollection();


			// Collection to populate and return.
			MessageHeaderCollection headers = new MessageHeaderCollection();

			string headersUnfolded = ParseHeaders(headersChunk);



			// Get the headers section of the message, unfolded as below loop assumes unfolding has happened.
			using (StringReader reader = new StringReader(headersUnfolded))
			{
				// Loop until break is hit (EOF).
				while (true)
				{
					// Get the next line from the stream.
					string line = reader.ReadLine();

					// If string is null or whitespace we have hit the end of the headers so break out.
					if (string.IsNullOrWhiteSpace(line))
						break;

					// Find the colon in the line.
					int colonIndex = line.IndexOf(":");
					// Grab the header name.
					string name = line.Substring(0, colonIndex);
					// Grab the header value.
					string value = line.Substring(colonIndex + 1);
					// Add the found header to the collection that will be returned.
					headers.Add(new MessageHeader(name, value));
				}
			}

			// Return the headers.
			return headers;
		}


		/// <summary>
		/// Gets the headers section of an Email.
		/// All lines before a blank line.
		/// </summary>
		/// <param name="headersBlock">The raw headers as a string.</param>
		/// <returns>The message header section or string.Empty if no header section in <paramref name="headersBlock"/>.</returns>
		private static string ParseHeaders(string headersBlock)
		{
			string str = headersBlock;

			// Might not be any headers, which is valid.
			if (string.IsNullOrWhiteSpace(str))
				return str;


			StringBuilder sb = null;
			using (StringReader reader = new StringReader(str))
			{
				// Get the first line.
				string line = reader.ReadLine();

				// Keep looping until we have gone through all of the lines in the header section.
				while (line != null)
				{
					// If first char of line is not white space then we need to add a new line 
					// as there is no wrapping.
					if (!string.IsNullOrWhiteSpace(line.Substring(0, 1)))
					{
						// If sb is null then this is the first line so create the stringbuilder.
						if (sb == null)
							sb = new StringBuilder();
						// Stringbuilder exists so we should add a new end of line.
						else
							sb.Append(MtaParameters.NewLine);
					}

					// Append the line to the string builder.
					sb.Append(line);

					// Get the next line.
					line = reader.ReadLine();
				}
			}

			// All unfolded so get our new string from the string builder (if it was created).
			if (sb != null)
				str = sb.ToString();
			else
				str = string.Empty;

			// Return the message header section.
			return str;
		}


		/// <summary>
		/// Attempts to split the headers and body of a MIME body part.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="headers"></param>
		/// <param name="body"></param>
		private static void SeparateBodyPartHeadersAndBody(string content, out string headers, out string body)
		{
			if (content.StartsWith(MtaParameters.NewLine))
			{
				// No headers, just a body.

				headers = string.Empty;

				// Remove the initial blank line to give the body.
				body = content.Substring(MtaParameters.NewLine.Length);

				return;
			}


			string separator = MtaParameters.NewLine + MtaParameters.NewLine;

			int splitAt = content.IndexOf(separator);

			if (splitAt == -1)
			{
				headers = string.Empty;
				body = content;
			}
			else
			{
				// Grab the header and body parts, remembering to remove the double CRLF between the two.
				headers = content.Substring(0, splitAt);
				body = content.Substring(splitAt + separator.Length);
			}
		}


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
				// This will be set to true when we are looking at a boundary line (may be folded). Don't wan't to unfold as it
				// will take quite a while.
				bool loopInBoundaryMarker = false;

				// This is used to hold the current body part as we step through the message.
				MimeMessageBodyPart currentBodyPart = null;

				// As long as there are lines keep looping.
				while (reader.Peek() > -1)
				{
					// Get the next line for us to work with.
					string line = reader.ReadLine();

					// If we are in a boundary marker check that we haven't exited it.
					if (loopInBoundaryMarker)
					{
						// Blank line signifies end of boundary marker.
						if (string.IsNullOrEmpty(line))
						{
							loopInBoundaryMarker = false;

							// Read the next line as we are now out of the boundary marker.
							line = reader.ReadLine();
						}
					}
					else
					{
						// If we aren't in a boundary marker look to see if we have entered one.
						if (line.StartsWith("--") && !line.EndsWith("--"))
						{
							// Get the current boundary name or use -- as that will match all bounderies.
							string boundary = currentBodyPart != null ? currentBodyPart.Boundary : "--";

							// Make sure the boundary is for this MIME message and not a child message.
							if (line.Contains(boundary))
							{
								currentBodyPart = new MimeMessageBodyPart();
								currentBodyPart.Boundary = line;
								bodyParts.Add(currentBodyPart);
								loopInBoundaryMarker = true;
							}
						}
						// We have reached the end of the message.
						else if (line.StartsWith("--") && line.EndsWith("--"))
							break;
					}

					// If we are in a boundary marker then check for conent- properties.
					if (loopInBoundaryMarker)
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

						// Look for the content type.  Default is "text/plain".
						m = Regex.Match(line, @"Content-Type:\s+(?<ContentType>\S+)", RegexOptions.IgnoreCase);
						if (m.Success)
							currentBodyPart.ContentType = new ContentType(m.Groups["ContentType"].Value);
						else
							currentBodyPart.ContentType = new ContentType();
					}
					// Not in a boundary marker so must be body part content.
					else if (currentBodyPart != null)
						currentBodyPart.EncodedBody += line + Environment.NewLine;
				}
			}

			return bodyParts.ToArray();
		}
	}
}
