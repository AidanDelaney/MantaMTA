using System.IO;
using System.Text;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// Extension methods for the StringReader class.
	/// </summary>
	public static class StringReaderExtensions
	{
		/// <summary>
		/// Reads up until a "\r\n" (CRLF), unlike the built-in ReadLine() which considers "\r", "\n", and "\r\n" as marking the end of a line.
		/// </summary>
		/// <returns></returns>
		public static string ReadToCrLf(this StringReader reader)
		{
			int c = reader.Read();

			// Are we at the end of the string?
			if (c == -1)
				return null;


			StringBuilder sb = new StringBuilder();

			// Move through the string one character at a time and checking we haven't reached the end
			// as indicated by a "\r\n" (CRLF).
			do
			{
				char ch = (char)c;

				// If the current character is a CR and the next character is an LF then
				// we've found the end of the line.
				if (ch == '\r' && ((char)reader.Peek() == '\n'))
				{
					// Advance the cursor to the second part of the CRLF so the next Read() will be
					// from after it.
					reader.Read();

					return sb.ToString();
				}
				else
				{
					sb.Append(ch);
				}
			} while ((c = reader.Read()) != -1);


			return sb.ToString();
		}
	}
}
