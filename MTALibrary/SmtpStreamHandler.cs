using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
		/// Stream reader for the underlying connection
		/// </summary>
		private StreamReader ClientStreamReader { get; set; }

		/// <summary>
		/// Stream writer for the underlying connection
		/// </summary>
		private StreamWriter ClientStreamWriter { get; set; }

		public SmtpStreamHandler(TcpClient client)
		{
			client.ReceiveTimeout = 30 * 1000;
			client.SendTimeout = 30 * 1000;

			this.RemoteAddress = (client.Client.RemoteEndPoint as IPEndPoint).Address;
			this.LocalAddress = (client.Client.LocalEndPoint as IPEndPoint).Address;

			// Don't need to use using on client stream as TcpClient will dispose it for us.
			this.ClientStreamReader = new StreamReader(client.GetStream());
			this.ClientStreamWriter = new StreamWriter(client.GetStream());
		}

		/// <summary>
		/// Read an SMTP line from the client.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public string ReadLine(bool log = true)
		{
			string response = ClientStreamReader.ReadLine();
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
			ClientStreamWriter.WriteLine(message);
			ClientStreamWriter.Flush();

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + message);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="msg"></param>
		/// <param name="log"></param>
		public void Write(string msg, bool log = true)
		{
			ClientStreamWriter.Write(msg);
			ClientStreamWriter.Flush();

			if (log)
				SmtpTransactionLogger.Instance.Log(", " + this.LocalAddress + ", " + this.RemoteAddress + ", Outbound, " + msg);
		}
	}
}
