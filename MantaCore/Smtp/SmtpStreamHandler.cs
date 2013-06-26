using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MantaMTA.Core.Enums;

namespace MantaMTA.Core.Smtp
{
	/// <summary>
	/// Makes it easy to work with TCP SMTP connections.
	/// </summary>
	public class SmtpStreamHandler
	{
		/// <summary>
		/// The local address is the address on the server that the client is connected to.
		/// </summary>
		public IPAddress LocalAddress { get; set; }
		/// <summary>
		/// The remote address is the source of the client request.
		/// </summary>
		public IPAddress RemoteAddress { get; set; }

		/// <summary>
		/// Stream reader for the underlying connection. Encoding is UTF8.
		/// </summary>
		private StreamReader ClientStreamReaderUTF8 { get; set; }

		/// <summary>
		/// Stream writer for the underlying connection. Encoding is UTF8.
		/// </summary>
		private StreamWriter ClientStreamWriterUTF8 { get; set; }

		/// <summary>
		/// Stream reader for the underlying connection. Encoding is 7bit ASCII.
		/// </summary>
		private StreamReader ClientStreamReaderASCII { get; set; }

		/// <summary>
		/// Stream writer for the underlying connection. Encoding is 7bit ASCII.
		/// </summary>
		private StreamWriter ClientStreamWriterASCII { get; set; }

		/// <summary>
		/// The SMTP Transport MIME currently set.
		/// </summary>
		private SmtpTransportMIME _CurrentTransportMIME { get; set; }

		/// <summary>
		/// Create an SmtpStreamHandler from the TCP client.
		/// </summary>
		/// <param name="client"></param>
		public SmtpStreamHandler(TcpClient client) : this(client.GetStream())
		{
			this.RemoteAddress = (client.Client.RemoteEndPoint as IPEndPoint).Address;
			this.LocalAddress = (client.Client.LocalEndPoint as IPEndPoint).Address;
		}

		/// <summary>
		/// Constructor is used for NUnit tests and SmtpStreamHandler(TcpClient).
		/// </summary>
		/// <param name="stream"></param>
		public SmtpStreamHandler(Stream stream)
		{
			this._CurrentTransportMIME = SmtpTransportMIME._7BitASCII;

			// Use new UTF8Encoding(false) so we don't send BOM to the network stream.
			this.ClientStreamReaderUTF8 = new StreamReader(stream, new UTF8Encoding(false));
			this.ClientStreamWriterUTF8 = new StreamWriter(stream, new UTF8Encoding(false));
			this.ClientStreamReaderASCII = new StreamReader(stream, Encoding.ASCII);
			this.ClientStreamWriterASCII = new StreamWriter(stream, Encoding.ASCII);
		}

		/// <summary>
		/// Set MIME type to be used for reading/writing the underlying stream.
		/// </summary>
		/// <param name="mime">Transport MIME to begin using.</param>
		public void SetSmtpTransportMIME(SmtpTransportMIME mime)
		{
			_CurrentTransportMIME = mime;
		}

		/// <summary>
		/// Read an SMTP line from the client.
		/// </summary>
		/// <param name="log">If true will log.</param>
		/// <returns>Line read from the stream.</returns>
		public async Task<string> ReadLineAsync(bool log = true)
		{
			string response = string.Empty;

			// Read the underlying stream using the correct encoding.
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
				response = await ClientStreamReaderASCII.ReadLineAsync();
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
				response = await ClientStreamReaderUTF8.ReadLineAsync();
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (response == null)
				throw new IOException("Remote Endpoint Disconnected.");

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Inbound, " + response);

			return response;
		}

		/// <summary>
		/// Reads all lines from the stream.
		/// </summary>
		/// <param name="log">If true will log.</param>
		/// <returns>All lines from the stream that are considered part of one message by SMTP.</returns>
		public string ReadAllLines(bool log = true)
		{
			return ReadAllLinesAsync(log).Result;
		}

		/// <summary>
		/// Reads all lines from the stream.
		/// </summary>
		/// <param name="log">If true will log.</param>
		/// <returns>All lines from the stream that are considered part of one message by SMTP.</returns>
		public async Task<string> ReadAllLinesAsync(bool log = true)
		{
			StringBuilder sb = new StringBuilder();

			string line = await ReadLineAsync(false);

			while (line[3] == '-')
			{
				sb.AppendLine(line);
				line = await ReadLineAsync(false);
			}
			sb.AppendLine(line);

			string result = sb.ToString();

			if (log)
				SmtpTransactionLogger.Instance.Log("," + this.LocalAddress + ", " + this.RemoteAddress + ", Inbound, " + result);

			return result;
		}

		/// <summary>
		/// Write a line to the Stream. Using the current transport MIME.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="log">If true will log.</param>
		public void WriteLine(string message, bool log = true)
		{
			Task.Run(() => WriteLineAsync(message, log)).Wait();
		}

		/// <summary>
		/// Write a line to the Stream. Using the current transport MIME.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="log">If true will log.</param>
		public async Task<bool> WriteLineAsync(string message, bool log = true)
		{
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
			{
				await ClientStreamWriterASCII.WriteLineAsync(message);
				await ClientStreamWriterASCII.FlushAsync();
			}
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
			{
				await ClientStreamWriterUTF8.WriteLineAsync(message);
				await ClientStreamWriterUTF8.FlushAsync();
			}
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + message);

			return true;
		}

		/// <summary>
		/// Write to the Stream. Using the current transport MIME.
		/// </summary>
		/// <param name="message">Message to send.</param>
		/// <param name="log">If true will log.</param>
		internal async Task<bool> WriteAsync(string message, bool log = true)
		{
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
			{
				await ClientStreamWriterASCII.WriteAsync(message);
				await ClientStreamWriterASCII.FlushAsync();
			}
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
			{
				await ClientStreamWriterUTF8.WriteAsync(message);
				await ClientStreamWriterUTF8.FlushAsync();
			}
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + message);

			return true;
		}
	}
}
