using MantaMTA.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;
using MantaMTA.Core.Message;

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
		public BodyPart[] BodyParts { get; set; }


		/// <summary>
		/// Creates a MimeMessage from a MIME formatted string.
		/// </summary>
		/// <param name="message">String content representing a MIME formatted email.</param>
		/// <returns>The MimeMessage represented by the string content in <paramref name="message"/>.</returns>
		public static MimeMessage Parse(string message)
		{
			try
			{
				MimeMessage msg = new MimeMessage();
				string headersChunk;
				string bodyChunk;
				SeparateBodyPartHeadersAndBody(message, out headersChunk, out bodyChunk);

				msg.Headers = GetMessageHeaders(headersChunk);

				// Check the email is a MIME message.
				if (msg.Headers.GetFirstOrDefault("MIME-Version") == null)
				{
					// TODO BenC (2013-07-25): Think this just means there's a single, plain text body part
					// so we add code here to handle it.

					Logging.Debug("Not a MIME Message");
					return null;
				}


				// Get the boundary of the message and use that to chop up the content, then loop through each
				// of those chunks looking for bodyparts within each.
				ContentType msgContentType = new ContentType(msg.Headers.GetFirstOrDefault("Content-Type").Value);

				// Content-Type: multipart/alternative; differences=Content-Type;
				//  boundary="b2420641-bd9f-4b3d-9a4f-c14c58c6ba30"
				// msgContentType.Value.Contains("boundary")


				// If no boundary, then there's just the one BodyPart in the content.
				if (string.IsNullOrWhiteSpace(msgContentType.Boundary))
					msg.BodyParts = new BodyPart[] { CreateBodyPart(msg.Headers, bodyChunk) };
				else
					msg.BodyParts = DivideIntoBodyParts(bodyChunk, msgContentType.Boundary);

				return msg;
			}
			catch (FormatException)
			{
				Logging.Warn("Failed to parse message");
				return null;
			}
		}


		/// <summary>
		/// Breaks up a string of MIME formatted content into one or more MimeMessageBodyPart objects.
		/// </summary>
		/// <param name="content">String content in MIME format.</param>
		/// <param name="boundary">String boundary marker that appears between body parts.</param>
		/// <returns>An array of MimeMessageBodyPart objects.</returns>
		private static BodyPart[] DivideIntoBodyParts(string content, string boundary)
		{
			List<BodyPart> bodyParts = new List<BodyPart>();

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
				// immediately after the boundary).
				p = p.StartsWith(MtaParameters.NewLine) ? p.Substring(MtaParameters.NewLine.Length) : p;



				// Got an actual body part.
				BodyPart bp = CreateBodyPart(p);


				bodyParts.Add(bp);
			}



			return bodyParts.ToArray();
		}


		/// <summary>
		/// Converts a string of MIME content into a MimeMessageBodyPart object, including
		/// any contained body parts if it is "multipart".
		/// </summary>
		/// <param name="content">MIME formatted content.</param>
		/// <returns>A MimeMessageBodyPart represented by <paramref name="content"/>.</returns>
		private static BodyPart CreateBodyPart(string content)
		{
			string headersChunk;
			string bodyChunk;

			// Split "headers" and "body".
			SeparateBodyPartHeadersAndBody(content, out headersChunk, out bodyChunk);



			// Get the key values from the headers that indicate how to handle this BodyPart.

			MessageHeaderCollection headers = GetMessageHeaders(headersChunk);
			BodyPart bp = CreateBodyPart(headers, bodyChunk);

			return bp;
		}


		/// <summary>
		/// Takes a collection of headers and a string of body content and creates a BodyPart from them.
		/// </summary>
		/// <param name="headers">A collection of headers relating to the content contained in <paramref name="bodyChunk"/>.</param>
		/// <param name="bodyChunk">A string of content for a BodyPart.</param>
		/// <returns>A BodyPart as represented by the headers and body content supplied.</returns>
		private static BodyPart CreateBodyPart(MessageHeaderCollection headers, string bodyChunk)
		{
			MessageHeader tempHeader = null;

			BodyPart bp = new BodyPart();
			bp.Headers = headers;


			tempHeader = headers.GetFirstOrDefault("Content-Transfer-Encoding");
			if (tempHeader != null)
				bp.TransferEncoding = IdentifyTransferEncoding(tempHeader.Value);
			else
				// Default is specified in RFCs as "7bit".
				bp.TransferEncoding = TransferEncoding.SevenBit;



			tempHeader = headers.GetFirstOrDefault("Content-Type");
			if (tempHeader != null)
			{
				bp.ContentType = new ContentType(tempHeader.Value);

				// Ensure we have a CharSet value - default is "us-ascii" according to RFCs.
				if (bp.ContentType.CharSet == null)
					bp.ContentType.CharSet = "us-ascii";
			}
			else
				// Default is specified in RFCs as "text/plain; charset=us-ascii".
				bp.ContentType = new ContentType("text/plain; charset=us-ascii");

			


			if (bp.ContentType.MediaType.StartsWith("multipart/"))
				// This bodypart is just a container for more bodyparts.
				bp.BodyParts = DivideIntoBodyParts(bodyChunk, bp.ContentType.Boundary);
			else
				// Nothing other than content contained within this bodypart.
				bp.EncodedBody = bodyChunk;


			return bp;
		}


		/// <summary>
		/// Converts the value from a Transfer-Encoding header (a string) into a 
		/// System.Net.Mime.TranserEncoding enum value.
		/// </summary>
		/// <param name="transferEncoding">The string value from a header, e.g. "base64"
		/// or quoted-printable.</param>
		/// <returns>The enum value represented by <paramref name="transferEncoding"/>, or TransferEncoding.Unknown
		/// if the value in <paramref name="transferEncoding"/> is not known.</returns>
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

			string headersUnfolded = UnfoldHeaders(headersChunk);



			// Get the headers section of the message, unfolded as below loop assumes unfolding has happened.
			using (StringReader reader = new StringReader(headersUnfolded))
			{
				// Loop until break is hit (EOF).
				while (true)
				{
					// Get the next line from the stream.
					string line = reader.ReadToCrLf();

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

			// Return the unfolded headers.
			return headers;
		}


		/// <summary>
		/// Unfolds all headers.
		/// </summary>
		/// <param name="headersBlock">The raw headers as a string with long lines folded.</param>
		/// <returns>The unfolded raw headers.</returns>
		public static string UnfoldHeaders(string headersBlock)
		{
			string str = headersBlock;

			// Might not be any headers, which is valid.
			if (string.IsNullOrWhiteSpace(str))
				return str;


			StringBuilder sb = null;
			using (StringReader reader = new StringReader(str))
			{
				// Get the first line.
				string line = reader.ReadToCrLf();

				// Keep looping until we have gone through all of the lines in the header section.
				while (line != null)
				{
					// If the line is blank or the first char is not white space then we need to add a new line 
					// as there is no wrapping.
					if (line.Length == 0 || !char.IsWhiteSpace(line[0]))
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
					line = reader.ReadToCrLf();
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
		internal static void SeparateBodyPartHeadersAndBody(string content, out string headers, out string body)
		{
			if (content.StartsWith(MtaParameters.NewLine))
			{
				// No headers, just a body.

				headers = string.Empty;

				// Remove the initial blank line to give the body.
				body = RemoveFunctionalBlankLinesAroundContent(content);	//.Substring(MtaParameters.NewLine.Length);

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
				body = RemoveFunctionalBlankLinesAroundContent(content.Substring(splitAt + separator.Length));
			}
		}


		/// <summary>
		/// Removes the functional blank lines that appear around content within a body part.
		/// Should be one before and one after, but this method will remove each independently.
		/// </summary>
		/// <param name="content">The MIME body part content.</param>
		/// <returns>The value in <paramref name="content"/> with one leading and one trailing
		/// blank line removed.</returns>
		private static string RemoveFunctionalBlankLinesAroundContent(string content)
		{
			string temp = content;

			if (temp.StartsWith(MtaParameters.NewLine))
				temp = temp.Substring(MtaParameters.NewLine.Length);


			if (content.EndsWith(MtaParameters.NewLine))
				temp = temp.Substring(0, temp.Length - MtaParameters.NewLine.Length);
			

			return temp;
		}
	}
}
