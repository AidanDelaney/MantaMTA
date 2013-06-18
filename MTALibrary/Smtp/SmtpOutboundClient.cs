using System;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.DNS;
using Colony101.MTA.Library.Enums;

namespace Colony101.MTA.Library.Smtp
{
	/// <summary>
	/// Handle connection with an SMTP Server.
	/// </summary>
	internal class SmtpOutboundClient : TcpClient, IDisposable
	{
		/// <summary>
		/// Will be false until Disposed is called
		/// </summary>
		private bool IsDisposed = false;

		/// <summary>
		/// Holds the MX Record that this client is connected to.
		/// </summary>
		public MXRecord MXRecord
		{
			get
			{
				return _MXRecord;
			}
		}
		private MXRecord _MXRecord { get; set; }

		/// <summary>
		/// SMTP stream handler, used to write/read the underlying stream.
		/// </summary>
		public SmtpStreamHandler SmtpStream { get; set; }

		/// <summary>
		/// Holds the Transport type that should be used for the DATA lines.
		/// </summary>
		private SmtpTransportMIME _DataTransportMime = SmtpTransportMIME._7BitASCII;

		/// <summary>
		/// Creates a SmtpOutboundClient bound to the specified endpoint.
		/// </summary>
		/// <param name="outboundEndpoint"></param>
		public SmtpOutboundClient(IPEndPoint outboundEndpoint) : base(outboundEndpoint) { }

		/// <summary>
		/// Finaliser, ensure dispose is always called.
		/// </summary>
		~SmtpOutboundClient()
		{
			if (!IsDisposed)
				this.Dispose(true);
		}

		/// <summary>
		/// Dispose method.
		/// </summary>
		public void Dispose() { this.Dispose(true); }

		/// <summary>
		/// Dispose method.
		/// </summary>
		/// <param name="disposing"></param>
		public new void Dispose(bool disposing)
		{
			if (IsDisposed || disposing)
				return;
			
			if (base.Connected)
				base.Close();

			base.Dispose(disposing);
		}

		/// <summary>
		/// Attempt to connect to the specified MX server.
		/// </summary>
		/// <param name="mx"></param>
		public void Connect(MXRecord mx)
		{
			base.Connect(mx.Host, 25);
			SmtpStream = new SmtpStreamHandler(this as TcpClient);
			_MXRecord = mx;

			// Read the Server greeting.
			string response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("2"))
			{
				// If the MX is actively denying use service access, SMTP code 421 then we should inform
				// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
				if (response.StartsWith("421"))
					ServiceNotAvailableManager.Add(SmtpStream.LocalAddress.ToString(), MXRecord.Host, DateTime.Now);

				base.Close();
				return;
			}
		}

		/// <summary>
		/// Say EHLO/HELO to the server.
		/// Will also check to see if 8BITMIME is supported.
		/// </summary>
		/// <param name="failedCallback">Action to call if hello fail.</param>
		public void ExecHelo(Action<string> failedCallback)
		{
			// Get the hostname of the IP address that we are connecting from.
			string hostname = System.Net.Dns.GetHostEntry(this.SmtpStream.LocalAddress).HostName;

			// We have connected to the MX, Say EHLO.
			SmtpStream.WriteLine("EHLO " + hostname);
			string response = SmtpStream.ReadAllLines();

			if (!response.StartsWith("2"))
			{
				// If server didn't respond with a success code on hello then we should retry with HELO
				SmtpStream.WriteLine("HELO " + hostname);
				response = SmtpStream.ReadAllLines();
				if (!response.StartsWith("250"))
				{
					failedCallback(response);
					base.Close();
				}
			}
			else
			{
				// Server responded to EHLO
				// Check to see if it supports 8BITMIME
				if (response.IndexOf("8BITMIME", StringComparison.OrdinalIgnoreCase) > -1)
					_DataTransportMime = SmtpTransportMIME._8BitUTF;
			}
		}

		/// <summary>
		/// Send the MAIL FROM command to the server using <paramref name="mailFrom"/> as parameter.
		/// </summary>
		/// <param name="mailFrom">Email address to use as parameter.</param>
		/// <param name="failed">Action to call if command fails.</param>
		public void ExecMailFrom(MailAddress mailFrom, Action<string> failedCallback)
		{
			SmtpStream.WriteLine("MAIL FROM: <" +
										(mailFrom == null ? string.Empty : mailFrom.Address) + ">" +
										(_DataTransportMime == SmtpTransportMIME._8BitUTF ? " BODY=8BITMIME" : string.Empty));
			string response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("250"))
				failedCallback(response);
		}

		/// <summary>
		/// Send the RCPT TO command to the server using <paramref name="rcptTo"/> as parameter.
		/// </summary>
		/// <param name="rcptTo">Email address to use as parameter.</param>
		/// <param name="failedCallback">Action to call if command fails.</param>
		public void ExecRcptTo(MailAddress rcptTo, Action<string> failedCallback)
		{
			SmtpStream.WriteLine("RCPT TO: <" + rcptTo.Address + ">");
			string response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("250"))
				failedCallback(response);
		}

		/// <summary>
		/// Send the data to the server
		/// </summary>
		/// <param name="data">Data to send to the server</param>
		/// <param name="failedCallback">Action to call if fails to send.</param>
		public void ExecData(string data, Action<string> failedCallback)
		{
			SmtpStream.WriteLine("DATA");
			string response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("354"))
			{
				failedCallback(response);
				return;
			}

			// Send the message data using the correct transport MIME
			SmtpStream.SetSmtpTransportMIME(_DataTransportMime);
			SmtpStream.Write(data, false);
			SmtpStream.Write(Environment.NewLine + "." + Environment.NewLine, false);

			// Data done so return to 7-Bit mode.
			SmtpStream.SetSmtpTransportMIME(SmtpTransportMIME._7BitASCII);


			response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("250"))
				failedCallback(response);
		}

		/// <summary>
		/// Send the SMTP Quit command to the Server.
		/// </summary>
		public void ExecQuit()
		{
			SmtpStream.WriteLine("QUIT");
		}
	}
}
