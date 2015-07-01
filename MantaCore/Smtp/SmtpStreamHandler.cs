using MantaMTA.Core.Enums;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MantaMTA.Core.Smtp
{
	/// <summary>
	/// Makes it easy to work with TCP SMTP connections.
	/// </summary>
	public class SmtpStreamHandler
	{
		/// <summary>
		/// Holds a Copy of a UTF8 Encoding without a BOM.
		/// </summary>
		private static Encoding _UTF8Encoding = new UTF8Encoding(false);

		/// <summary>
		/// The local address is the address on the server that the client is connected to.
		/// </summary>
		public IPAddress LocalAddress { get; set; }
		/// <summary>
		/// The port number connected to at the local address.
		/// </summary>
		public int LocalPort { get; set; }
		/// <summary>
		/// The remote address is the source of the client request.
		/// </summary>
		public IPAddress RemoteAddress { get; set; }
		/// <summary>
		/// The port number connected to at the remote address.
		/// </summary>
		public int RemotePort { get; set; }

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
			IPEndPoint remote = client.Client.RemoteEndPoint as IPEndPoint;			
			this.RemoteAddress = remote.Address;
			this.RemotePort = remote.Port;

			IPEndPoint local = client.Client.LocalEndPoint as IPEndPoint;
			this.LocalAddress = local.Address;
			this.LocalPort = local.Port;
		}

		/// <summary>
		/// Constructor is used for NUnit tests and SmtpStreamHandler(TcpClient).
		/// </summary>
		/// <param name="stream"></param>
		public SmtpStreamHandler(Stream stream)
		{
			this._CurrentTransportMIME = SmtpTransportMIME._7BitASCII;

			// Use new UTF8Encoding(false) so we don't send BOM to the network stream.
			this.ClientStreamReaderUTF8 = new StreamReader(stream, _UTF8Encoding);
			this.ClientStreamWriterUTF8 = new StreamWriter(stream, _UTF8Encoding);
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
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ":" + this.LocalPort + ", " + this.RemoteAddress + ":" + this.RemotePort + ", Inbound, " + response);

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

			string line = string.Empty;
			try
			{
				line = await ReadLineAsync(false);
			}
			catch (Exception)
			{
				return line;
			}

			while (line[3] == '-')
			{
				sb.AppendLine(line);
				try
				{
					line = await ReadLineAsync(false);
				}
				catch (Exception)
				{
					line = string.Empty;
					break;
				}
			}
			sb.AppendLine(line);

			string result = sb.ToString();

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ":" + this.LocalPort + ", " + this.RemoteAddress + ":" + this.RemotePort + ", Inbound, " + result);

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
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ":" + this.LocalPort + ", " + this.RemoteAddress + ":" + this.RemotePort + ", Outbound, " + message);

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
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ":" + this.LocalPort + ", " + this.RemoteAddress + ":" + this.RemotePort + ", Outbound, " + message);

			return true;
		}
	}
}
