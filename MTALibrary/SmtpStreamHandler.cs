using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Colony101.MTA.Library.Enums;

namespace Colony101.MTA.Library
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

		public SmtpStreamHandler(TcpClient client)
		{
			client.ReceiveTimeout = 30 * 1000;
			client.SendTimeout = 30 * 1000;


			this.RemoteAddress = (client.Client.RemoteEndPoint as IPEndPoint).Address;
			this.LocalAddress = (client.Client.LocalEndPoint as IPEndPoint).Address;

			this._CurrentTransportMIME = SmtpTransportMIME._7BitASCII;

			this.ClientStreamReaderUTF8 = new StreamReader(client.GetStream(), new UTF8Encoding(false));
			this.ClientStreamWriterUTF8 = new StreamWriter(client.GetStream(), new UTF8Encoding(false));
			this.ClientStreamReaderASCII = new StreamReader(client.GetStream(), Encoding.ASCII);
			this.ClientStreamWriterASCII = new StreamWriter(client.GetStream(), Encoding.ASCII);
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
		/// <param name="client"></param>
		/// <returns></returns>
		public string ReadLine(bool log = true)
		{
			string response = string.Empty;

			// Read the underlying stream using the correct encoding.
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
				response = ClientStreamReaderASCII.ReadLine();
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
				response = ClientStreamReaderUTF8.ReadLine();
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (response == null)
				throw new IOException("Remote Endpoint Disconnected.");

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Inbound, " + response);

			return response;
		}

		/// <summary>
		/// Read SMTP response until last line is returned.
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		public string ReadAllLines(bool log = true)
		{
			StringBuilder sb = new StringBuilder();

			string line = ReadLine(false);

			while (line[3] == '-')
			{
				sb.AppendLine(line);
				line = ReadLine(false);
			}
			sb.AppendLine(line);

			string result = sb.ToString();

			if (log)
				SmtpTransactionLogger.Instance.Log("," + this.LocalAddress + ", " + this.RemoteAddress + ", Inbound, " + result);

			return result;
		}

		/// <summary>
		/// Send an SMTP line to the client
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		public void WriteLine(string message, bool log = true)
		{
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
			{
				ClientStreamWriterASCII.WriteLine(message);
				ClientStreamWriterASCII.Flush();
			}
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
			{
				ClientStreamWriterUTF8.WriteLine(message);
				ClientStreamWriterUTF8.Flush();
			}
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + message);
		}

		internal void Write(string message, bool log = true)
		{
			if (_CurrentTransportMIME == SmtpTransportMIME._7BitASCII)
			{
				ClientStreamWriterASCII.Write(message);
				ClientStreamWriterASCII.Flush();
			}
			else if (_CurrentTransportMIME == SmtpTransportMIME._8BitUTF)
			{
				ClientStreamWriterUTF8.Write(message);
				ClientStreamWriterUTF8.Flush();
			}
			else
				throw new NotImplementedException(_CurrentTransportMIME.ToString());

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + message);
		}
	}
}
