﻿using System;
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
		/// Timer to use to cause idle timeouts. Need this as connections will be left
		/// open after message sent. Should self quit after period.
		/// </summary>
		private SmtpClientTimeoutTimer _IdleTimeoutTimer { get; set; }

		/// <summary>
		/// Count of the data commands sent by this client.
		/// </summary>
		private int _DataCommands = 0;

		/// <summary>
		/// Creates a SmtpOutboundClient bound to the specified endpoint.
		/// </summary>
		/// <param name="outboundEndpoint"></param>
		public SmtpOutboundClient(IPEndPoint outboundEndpoint) : base(outboundEndpoint) 
		{
			base.ReceiveTimeout = MtaParameters.Client.CONNECTION_RECEIVE_TIMEOUT_INTERVAL * 1000;
			base.SendTimeout = MtaParameters.Client.CONNECTION_SEND_TIMEOUT_INTERVAL * 1000;
			base.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		}

		/// <summary>
		/// 
		/// </summary>
		private bool _HasHelloed = false;

		/// <summary>
		/// Finaliser, ensure dispose is always called.
		/// </summary>
		~SmtpOutboundClient()
		{
			if (base.Connected)
				ExecQuit();

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
			base.Dispose(disposing);
		}

		/// <summary>
		/// Attempt to connect to the specified MX server.
		/// </summary>
		/// <param name="mx"></param>
		public void Connect(MXRecord mx)
		{
			base.Connect(mx.Host, MtaParameters.Client.SMTP_PORT);
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

			// Were connected so setup the idle timeout.
			// Quits the connection nicely if it isn't being used.
			_IdleTimeoutTimer = new SmtpClientTimeoutTimer(MtaParameters.Client.CONNECTION_IDLE_TIMEOUT_INTERVAL, ExecQuit);
			_IdleTimeoutTimer.Start();
		}

		/// <summary>
		/// Say EHLO/HELO to the server.
		/// Will also check to see if 8BITMIME is supported.
		/// </summary>
		/// <param name="failedCallback">Action to call if hello fail.</param>
		public void ExecHelo(Action<string> failedCallback)
		{
			if (!base.Connected)
				return;

			_IdleTimeoutTimer.Stop();

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

			_HasHelloed = true;
			_IdleTimeoutTimer.Start();
		}

		/// <summary>
		/// Send the MAIL FROM command to the server using <paramref name="mailFrom"/> as parameter.
		/// </summary>
		/// <param name="mailFrom">Email address to use as parameter.</param>
		/// <param name="failed">Action to call if command fails.</param>
		public void ExecMailFrom(MailAddress mailFrom, Action<string> failedCallback)
		{
			if (!base.Connected)
				return;

			_IdleTimeoutTimer.Stop();

			SmtpStream.WriteLine("MAIL FROM: <" +
										(mailFrom == null ? string.Empty : mailFrom.Address) + ">" +
										(_DataTransportMime == SmtpTransportMIME._8BitUTF ? " BODY=8BITMIME" : string.Empty));
			string response = SmtpStream.ReadAllLines();
			_IdleTimeoutTimer.Start();
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
			if (!base.Connected)
				return;

			_IdleTimeoutTimer.Stop();

			SmtpStream.WriteLine("RCPT TO: <" + rcptTo.Address + ">");
			
			string response = SmtpStream.ReadAllLines();
			
			_IdleTimeoutTimer.Start();

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
			if (!base.Connected)
				return;

			_IdleTimeoutTimer.Stop();

			SmtpStream.WriteLine("DATA");
			string response = SmtpStream.ReadAllLines();
			if (!response.StartsWith("354"))
			{
				failedCallback(response);
				return;
			}

			// Increment the data commands as server has responded positiely.
			_DataCommands++;

			// Send the message data using the correct transport MIME
			SmtpStream.SetSmtpTransportMIME(_DataTransportMime);
			SmtpStream.Write(data, false);
			SmtpStream.Write(MtaParameters.NewLine + "." + MtaParameters.NewLine, false);

			// Data done so return to 7-Bit mode.
			SmtpStream.SetSmtpTransportMIME(SmtpTransportMIME._7BitASCII);


			response = SmtpStream.ReadAllLines();
			_IdleTimeoutTimer.Start();


			if (!response.StartsWith("250"))
				failedCallback(response);


			// If max messages have been sent quit the connection.			
			if (_DataCommands >= 5) // Will use rules in the future, just hardcode now.
				ExecQuit();
		}

		/// <summary>
		/// Send the SMTP Quit command to the Server.
		/// </summary>
		public void ExecQuit()
		{
			if (!base.Connected)
				return;

			_IdleTimeoutTimer.Stop();
			SmtpStream.WriteLine("QUIT");
			// Don't read response as don't care.
			// Close the TCP connection.
			base.Close();
		}

		/// <summary>
		/// Send the RSET command to the server.
		/// </summary>
		public void ExecRset()
		{
			if (!base.Connected)
			{
				Logging.Debug("Cannot RSET connection has been closed.");
				throw new Exception();
			}
			_IdleTimeoutTimer.Stop();
			SmtpStream.WriteLine("RSET");
			SmtpStream.ReadAllLines();
			_IdleTimeoutTimer.Start();
		}

		/// <summary>
		/// HELO or RSET depending on previous commands.
		/// </summary>
		/// <param name="failedCallback"></param>
		public void ExecHeloOrRset(Action<string> failedCallback)
		{
			if (!_HasHelloed)
				ExecHelo(failedCallback);
			else
				ExecRset();
		}
	}

	/// <summary>
	/// Class is used to cause idle timeouts.
	/// </summary>
	internal class SmtpClientTimeoutTimer
	{
		/// <summary>
		/// Internal timer.
		/// </summary>
		private System.Timers.Timer _Timer { get; set; }

		/// <summary>
		/// Creates a timer that will call the specified action after interval.
		/// </summary>
		/// <param name="interval">Seconds idle before timeout (seconds).</param>
		/// <param name="timeoutAction">Action to call on timeout.</param>
		public SmtpClientTimeoutTimer(int interval, Action timeoutAction)
		{
			_Timer = new System.Timers.Timer(interval * 1000);
			_Timer.Elapsed += new System.Timers.ElapsedEventHandler(
				delegate(object sender, System.Timers.ElapsedEventArgs e)
				{
					Logging.Debug("TCP Client Timeout");
					timeoutAction();
				});
		}

		/// <summary>
		/// Start the Timer.
		/// </summary>
		public void Start()
		{
			_Timer.Start();
		}

		/// <summary>
		/// Stop the Timer.
		/// </summary>
		public void Stop()
		{
			_Timer.Stop();
		}
	}
}