using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Message
{
	/// <summary>
	/// Manager provides methods for working with Email Message Headers.
	/// </summary>
	public static class MessageManager
	{
		/// <summary>
		/// The default message line length for email messages.
		/// </summary>
		private const int DEFAULT_MESSAGE_LINE_LENGTH = 78;

		/// <summary>
		/// Add the header to the Email.
		/// </summary>
		/// <param name="message">The original email message.</param>
		/// <param name="header">The header to add to the email message.</param>
		/// <returns>The original message with the new header.</returns>
		public static string AddHeader(string message, MessageHeader header)
		{
			string headingPart = GetHeaderSection(message, false);
			string bodyPart = GetMessageBodySection(message);

			return string.Format("{0}{1}{2}{2}{3}", FoldHeader(header, DEFAULT_MESSAGE_LINE_LENGTH), headingPart, MtaParameters.NewLine, bodyPart);
		}

		/// <summary>
		/// Removes the specified header from the email message.
		/// </summary>
		/// <param name="message">Original email message.</param>
		/// <param name="headerName">Name of the header to remove from the message.</param>
		/// <returns>The original message with the header removed.</returns>
		public static string RemoveHeader(string message, string headerName)
		{
			// Grab the header section of the message. Don't unfold as we don't want
			// to change any headers other than the one we are removing.
			string headerSection = GetHeaderSection(message, false);

			// String build will be used to build a new header section.
			StringBuilder sbNewHeaderSection = new StringBuilder(string.Empty);

			using (StringReader reader = new StringReader(headerSection))
			{
				// Will hold a single line from the header section. (Unfolded).
				string line = string.Empty;


				// Keep looping until we have gone through all of the lines in the header section.
				do
				{
					// Get the first line.
					line = reader.ReadLine();

					// If the line is null we can't do anything.
					if (string.IsNullOrEmpty(line))
						continue;

					// If the first line is whitespace or there is no : char then there is no header name
					// to check so keep this line.
					if (char.IsWhiteSpace(line[0]) || line.IndexOf(":") < 0)
					{
						sbNewHeaderSection.Append(line + MtaParameters.NewLine);
						continue;
					}

					// We have found a header name in the line, get it so we can check it.
					string chname = line.Substring(0, line.IndexOf(":")).TrimEnd();

					// If the found header name doesn't match the name of the header that we are removing
					// from the email message then keep the line.
					if (!chname.Equals(headerName, StringComparison.OrdinalIgnoreCase))
					{
						sbNewHeaderSection.Append(line + MtaParameters.NewLine);
						continue;
					}

					//
					// FOUND THE HEADER TO REMOVE!!!
					//
 					// We remove the header by simply not adding it to the new header section.
					// We need to peek at the next line and move through the header section
					// until we find the next header.
					while (char.IsWhiteSpace((char)reader.Peek()))
						reader.ReadLine();

				} while (!string.IsNullOrWhiteSpace(line));
			}
			
			// Return the new header section with the original body.
			return sbNewHeaderSection.ToString() + MtaParameters.NewLine + GetMessageBodySection(message);
		}

		/// <summary>
		/// Folds the specified message header.
		/// </summary>
		/// <param name="header">Header to fold.</param>
		/// <param name="maxLength">The maximum length of the line.</param>
		/// <returns>The folded header.</returns>
		internal static string FoldHeader(MessageHeader header, int maxLength)
		{
			// Calculate the maximum line length without CLRF
			int maxLengthBeforeCLRF = maxLength - MtaParameters.NewLine.Length;
			
			// Build the header string from the message header object.
			string str = (header.Name + ": " + header.Value.TrimStart()).Replace(MtaParameters.NewLine, string.Empty);

			// If the header lenght is less that the max line lenght no folding 
			// is required and it can just be returned.
			if (str.Length < maxLengthBeforeCLRF)
				return str + MtaParameters.NewLine;

			// StringBuild will be used to build the folder header.
			StringBuilder foldedHeader = new StringBuilder();

			while (str.Length > 0)
			{
				if (str.Length < maxLengthBeforeCLRF)
				{
					// String is already under the max line length.
					foldedHeader.Append(str + MtaParameters.NewLine);
					str = string.Empty;
				}
				else
				{
					// String is too long for the line.

					// Get the max line length substring so we can work back and find some whitespace to split on.
					string subStr = str.Substring(0, maxLengthBeforeCLRF);
					
					// Set to true if a whitespace char is found.
					bool foundWhitespace = false;
					
					// Set to true if a split char is found.
					bool foundSplitChar = false;

					// We need to treat any whitespace at the start of the string as if they weren't whitespace.
					// Do this by only stepping back to the first non-whitespace char in the string.
					int firstNonWhitespace = 0;

					// Find the first non-whitespace char.
					for (int i = 0; i < subStr.Length; i++)
					{
						if (!char.IsWhiteSpace(subStr[i]))
						{
							firstNonWhitespace = i;
							break;
						}
					}

					// This will be set to the index of a foldable position in the string, if one is found.
					int foldPos = -1;

					// Starting from the end of the string loopback
					for (int i = subStr.Length - 1; i > firstNonWhitespace; i--)
					{
						char c = subStr[i];

						if (char.IsWhiteSpace(c))
						{
							foundWhitespace = true;
							foldPos = i;
							continue;
						}
						else if (c.Equals(';') && (i + 1) < subStr.Length)
						{
							foundSplitChar = true;
							foldPos = i + 1;
							continue;
						}
						else if (foundWhitespace || foundSplitChar)
						{
							foldedHeader.Append(subStr.Substring(0, foldPos) + MtaParameters.NewLine);

							if (foundWhitespace)
								str = str.Remove(0, foldPos);
							else
								str = " " + str.Remove(0, foldPos);

							// Reset the max line length as it may have been increased for a single line.
							maxLengthBeforeCLRF = maxLength - MtaParameters.NewLine.Length;

							break;
						}
						else if (i == firstNonWhitespace + 1)
						{
							// Found a line too big for size, likly DKIM unfoldable at current size incress current line to 1,000
							if (maxLengthBeforeCLRF == 1000 - MtaParameters.NewLine.Length)
								throw new Exception("Unfoldable Reject Email");
							maxLengthBeforeCLRF = 1000 - MtaParameters.NewLine.Length;
							break;
						}
					}
				}
			}


			return foldedHeader.ToString();
		}

		/// <summary>
		/// Gets a collection of the message headers from the raw messageData.
		/// </summary>
		/// <param name="messageData">The raw message data.</param>
		/// <returns>Collection of headers.</returns>
		public static MessageHeaderCollection GetMessageHeaders(string messageData)
		{
			// Collection to populate and return.
			MessageHeaderCollection headers = new MessageHeaderCollection();

			// Get the headers section of the message, unfolded as below loop assumes unfolding has happened.
			using (StringReader reader = new StringReader(GetHeaderSection(messageData, true)))
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
		/// Get the header section unfolded.
		/// </summary>
		/// <param name="headerSection">The messages header section.</param>
		/// <returns><paramref name="headerSection"/> unfolded.</returns>
		public static string UnfoldHeaders(string headerSection)
		{
			StringBuilder sb = null;
			using (StringReader reader = new StringReader(headerSection))
			{
				// Get the first line.
				string line = reader.ReadLine();

				// Keep looping until we have gone through all of the lines in the header section.
				while (!string.IsNullOrWhiteSpace(line))
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

			// All unfolded so get our new string from the string builder.
			return sb.ToString();
		}

		/// <summary>
		/// Gets the headers section of an Email.
		/// All lines before a blank line.
		/// </summary>
		/// <param name="messageData">The raw message data.</param>
		/// <param name="unfold">If true will unfold headers into a single line</param>
		/// <returns>The message header section or string.Empty if no header section in messageData.</returns>
		private static string GetHeaderSection(string messageData, bool unfold)
		{
			int endOfHeadersIndex = messageData.IndexOf(MtaParameters.NewLine + MtaParameters.NewLine);
			
			// There are no headers so return an empty string.
			if (endOfHeadersIndex < 0)
				return string.Empty;

			// Strip the body so we are only working with the headers.
			string str = messageData.Substring(0, endOfHeadersIndex);

			if (unfold)
				str = UnfoldHeaders(str);

			// Return the message header section.
			return str;
		}

		/// <summary>
		/// Gets the body section of the Email Messages. Bit after the headers.
		/// </summary>
		/// <param name="messageData">Raw message DATA</param>
		/// <returns></returns>
		internal static string GetMessageBodySection(string messageData)
		{
			int endOfHeadersIndex = messageData.IndexOf(MtaParameters.NewLine + MtaParameters.NewLine);

			// There are no headers so the whole thing is the body.
			if (endOfHeadersIndex < 0)
				return messageData;

			// Extract the body.
			messageData = messageData.Substring(endOfHeadersIndex + (MtaParameters.NewLine + MtaParameters.NewLine).Length);

			// Return the extracted body.
			return messageData;
		}
	}

	/// <summary>
	/// Class represents an Email message header.
	/// </summary>
	public class MessageHeader
	{
		/// <summary>
		/// Name of the message header.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Message header value.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Create a new MessageHeader object.
		/// </summary>
		/// <param name="name">Name of the Header.</param>
		/// <param name="value">Headers value.</param>
		public MessageHeader(string name, string value)
		{
			Name = name.Trim();
			Value = value.Trim();
		}
	}

	/// <summary>
	/// Holds a collection of Email message headers.
	/// </summary>
	public class MessageHeaderCollection : List<MessageHeader>
	{
		public MessageHeaderCollection() { }
		public MessageHeaderCollection(IEnumerable<MessageHeader> collection) : base(collection) { }


		/// <summary>
		/// Retrieves all MessageHeaders found with the Name provided in <paramref name="header"/>.
		/// Search is case-insensitive.
		/// </summary>
		/// <param name="header">The Name of the header to find.  The case of it is not important.</param>
		/// <returns>A MessageHeaderCollection of matches, if any.</returns>
		public MessageHeaderCollection GetAll(string header)
		{
			return new MessageHeaderCollection(this.Where(h => h.Name.Equals(header, StringComparison.OrdinalIgnoreCase)));
		}


		/// <summary>
		/// Retrives the first MessageHeader found with the Name provided in <paramref name="header"/>.
		/// </summary>
		/// <param name="header">The Name of the header to find.  The case of it is not important.</param>
		/// <returns>A MessageHeader object for the first match, else null if no matches were found.</returns>
		public MessageHeader GetFirstOrDefault(string header)
		{
			return this.FirstOrDefault(h => h.Name.Equals(header, StringComparison.OrdinalIgnoreCase));
		}
	}

	/// <summary>
	/// Names of the MTA Headers.
	/// </summary>
	internal class MessageHeaderNames
	{
		/// <summary>
		/// First bit of control/command headers.
		/// </summary>
		public const string HeaderNamePrefix = "X-" + MtaParameters.MTA_NAME + "-";
		
		/// <summary>
		/// The send group ID header name.
		/// Used to pass in the Send Group.
		/// </summary>
		public const string SendGroupID = HeaderNamePrefix + "VMtaGroupID";

		/// <summary>
		/// The SendID is used by external systems to identify the send.
		/// </summary>
		public const string SendID = HeaderNamePrefix + "SendID";

		/// <summary>
		/// The return path domain is used to pass in the return path.
		/// </summary>
		public const string ReturnPathDomain = HeaderNamePrefix + "ReturnPathDomain";
	}
}
