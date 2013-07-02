using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MantaMTA.Core.Server
{
	/// <summary>
	/// Manager provides methods for working with Email Message Headers.
	/// </summary>
	internal static class MessageHeaderManager
	{
		/// <summary>
		/// Replaces the headers in the messageData with the new ones.
		/// </summary>
		/// <param name="newHeaders">The new headers to be used to replace the current headers.</param>
		/// <returns>Message string containing the new headers</returns>
		public static string ReplaceHeaders(string messageData, MessageHeaderCollection newHeaders)
		{
			StringBuilder sb = new StringBuilder(string.Empty);
			for (int i = 0; i < newHeaders.Count; i++)
			{
				MessageHeader cHeader = newHeaders[i];

				sb.Append(GetFoldedHeader(cHeader, 78));
			}

			sb.Append(MtaParameters.NewLine);

			sb.Append(GetMessageBodySection(messageData));

			return sb.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="header"></param>
		/// <param name="maxLength"></param>
		/// <returns></returns>
		private static string GetFoldedHeader(MessageHeader header, int maxLength)
		{
			// Calculate the maximum line length without CLRF
			int maxLengthBeforeCLRF = maxLength - MtaParameters.NewLine.Length;
			string str = (header.Name + ": " + header.Value.TrimStart()).Replace(MtaParameters.NewLine, string.Empty);

			if (str.Length < maxLengthBeforeCLRF)
				return str + MtaParameters.NewLine;

			StringBuilder allLines = new StringBuilder();

			while (str.Length > 0)
			{
				if (str.Length < maxLengthBeforeCLRF)
				{
					allLines.Append(str + MtaParameters.NewLine);
					str = string.Empty;
				}
				else
				{
					string subStr = str.Substring(0, maxLengthBeforeCLRF);
					bool foundWhitespace = false;
					bool foundSplitChar = false;

					int foldPos = -1;
					for (int i = subStr.Length - 1; i > 0; i--)
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
							allLines.Append(subStr.Substring(0, foldPos) + MtaParameters.NewLine);

							if (foundWhitespace)
								str = str.Remove(0, foldPos);
							else
								str = " " + str.Remove(0, foldPos);
							
							break;
						}
						else if (i == 1)
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


			return allLines.ToString();
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
			{
				StringBuilder sb = null;
				using (StringReader reader = new StringReader(str))
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
				str = sb.ToString();
			}

			// Return the message header section.
			return str;
		}

		/// <summary>
		/// Gets the body section of the Email Messages. Bit after the headers.
		/// </summary>
		/// <param name="messageData">Raw message DATA</param>
		/// <returns></returns>
		private static string GetMessageBodySection(string messageData)
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
	internal class MessageHeader
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
	internal class MessageHeaderCollection : List<MessageHeader>
	{
		public MessageHeaderCollection() { }
		public MessageHeaderCollection(IEnumerable<MessageHeader> collection) : base(collection) { }
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
		public const string SendGroupID = HeaderNamePrefix + "SendGroupID";

		/// <summary>
		/// The SendID is used by external systems to identify the send.
		/// </summary>
		public const string SendID = HeaderNamePrefix + "SendID";
	}
}
