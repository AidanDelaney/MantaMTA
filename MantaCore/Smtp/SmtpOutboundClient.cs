using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MantaMTA.Core.Client;
using MantaMTA.Core.DNS;
using MantaMTA.Core.Enums;

namespace MantaMTA.Core.Smtp
{
	/// <summary>
	/// Singleton object will hold all connections that MantaMTA creates. If ensure that connections are closed when they are finished with.
	/// </summary>
	internal class SmtpOutboundClientCollection : List<SmtpOutboundClient>
	{
		/// <summary>
		/// The actual instance of the SmtpOutboundClientCollection
		/// </summary>
		private static SmtpOutboundClientCollection _Instance = new SmtpOutboundClientCollection();
		/// <summary>
		/// Gets the instance of the SmtpOutboundClientCollection
		/// </summary>
		public static SmtpOutboundClientCollection Instance { get { return _Instance; } }
		
		private SmtpOutboundClientCollection() : base()
		{
			// Thread responsable for removing inactive connections.
			Thread t = new Thread(new ThreadStart(delegate()
			{
				// Loop forever
				while (true)
				{
					try
					{
						int removedCount = 0;

						// Loop through all of the connections.
						for (int i = 0; i < _Instance.Count; i++)
						{
							SmtpOutboundClient client = _Instance[i];
							if (client != null && 
								client.IsActive == false &&
								client.LastActive.AddSeconds(MtaParameters.Client.ConnectionIdleTimeoutInterval) <= DateTime.UtcNow)
							{
								// The connection is not null and appears to have been inactive for the idle timeout interval.

								try
								{
									if (!client.ExecQuitAsync().Result)
									{
										if(client.Connected)
											client.GetStream().Close();
										client.Close();
									}
								}
								catch (Exception) 
								{
									client.Close();
								}
								i--; // ExecQuitAsync will remove from the list.
								removedCount++;
							}
						}

						if (removedCount > 0)
							Logging.Debug("Removed " + removedCount + " idle connections");
					}
					catch (Exception ex)
					{
						Logging.Fatal("SmtpOutboundClient idle handler failed", ex);
						MantaCoreEvents.InvokeMantaCoreStopping();
						Environment.Exit(-1);
					}

					Thread.Sleep(1000);
				}
			}));
			t.IsBackground = true;
			t.Start();
		}
	}

	/// <summary>
	/// Handle connection with an SMTP Server.
	/// </summary>
	internal class SmtpOutboundClient : TcpClient, IDisposable
	{
		private DateTime _LastActive = DateTime.UtcNow;
		public DateTime LastActive
		{
			get	{ return _LastActive; }
			set	{ _LastActive = value; }
		}

		/// <summary>
		/// Set to true when code is using this connection.
		/// </summary>
		public bool IsActive { get; set; }


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
		/// Count of the DATA commands sent by this client.
		/// </summary>
		private int _DataCommands = 0;

		/// <summary>
		/// Is false until HELO or EHLO has been sent to the server.
		/// </summary>
		private bool _HasHelloed = false;

		/// <summary>
		/// Holds the MTA IP Address this client is using.
		/// </summary>
		private VirtualMta.VirtualMTA MtaIpAddress { get; set; }

		/// <summary>
		/// Creates a SmtpOutboundClient bound to the specified endpoint.
		/// </summary>
		/// <param name="ipAddress">The local IP address to bind to.</param>
		public SmtpOutboundClient(VirtualMta.VirtualMTA ipAddress) : base(new IPEndPoint(ipAddress.IPAddress, 0)) 
		{
			this.IsActive = true;
			this.MtaIpAddress = ipAddress;
			base.ReceiveTimeout = MtaParameters.Client.ConnectionReceiveTimeoutInterval * 1000;
			base.SendTimeout = MtaParameters.Client.ConnectionSendTimeoutInterval * 1000;
			base.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			SmtpOutboundClientCollection.Instance.Add(this);
			this.IsActive = false;
		}

		/// <summary>
		/// Finaliser, ensure dispose is always called.
		/// </summary>
		~SmtpOutboundClient()
		{
			if (base.Connected)
				Task.Run(()=>ExecQuitAsync()).Wait();

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
			SmtpOutboundClientCollection.Instance.Remove(this);
		}

		/// <summary>
		/// Attempt to connect to the specified MX server.
		/// </summary>
		/// <param name="mx">MX Record of the server to connect to.</param>
		public void Connect(MXRecord mx)
		{
			_LastActive = DateTime.UtcNow;
			IsActive = true;
			base.Connect(mx.Host, MtaParameters.Client.SMTP_PORT);
			_LastActive = DateTime.UtcNow;
			SmtpStream = new SmtpStreamHandler(this as TcpClient);
			_MXRecord = mx;

			// Read the Server greeting.
			string response = SmtpStream.ReadAllLines();
			_LastActive = DateTime.UtcNow;

			if (!response.StartsWith("2"))
			{
				// If the MX is actively denying use service access, SMTP code 421 then we should inform
				// the ServiceNotAvailableManager manager so it limits our attepts to this MX to 1/minute.
				if (response.StartsWith("421"))
					ServiceNotAvailableManager.Add(SmtpStream.LocalAddress.ToString(), MXRecord.Host, DateTime.UtcNow);

				base.Close();
				return;
			}

			IsActive = false;
			_LastActive = DateTime.UtcNow;
		}

		/// <summary>
		/// Say EHLO/HELO to the server.
		/// Will also check to see if 8BITMIME is supported.
		/// </summary>
		/// <param name="failedCallback">Action to call if hello fail.</param>
		public async Task<bool> ExecHeloAsync(Action<string> failedCallback)
		{
			if (!base.Connected)
				return false;

			_LastActive = DateTime.UtcNow;
			IsActive = true;

			// Get the hostname of the IP address that we are connecting from.
			string hostname = System.Net.Dns.GetHostEntry(this.SmtpStream.LocalAddress).HostName;

			// We have connected to the MX, Say EHLO.
			_LastActive = DateTime.UtcNow;
			await SmtpStream.WriteLineAsync("EHLO " + hostname);
			string response = await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;

			if (!response.StartsWith("2"))
			{
				// If server didn't respond with a success code on hello then we should retry with HELO
				await SmtpStream.WriteLineAsync("HELO " + hostname);
				response = await SmtpStream.ReadAllLinesAsync();
				_LastActive = DateTime.UtcNow;
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
			_LastActive = DateTime.UtcNow;
			IsActive = false;
			return true;
		}

		/// <summary>
		/// Send the MAIL FROM command to the server using <paramref name="mailFrom"/> as parameter.
		/// </summary>
		/// <param name="mailFrom">Email address to use as parameter.</param>
		/// <param name="failed">Action to call if command fails.</param>
		public async Task<bool> ExecMailFromAsync(MailAddress mailFrom, Action<string> failedCallback)
		{
			if (!base.Connected)
				return false;

			_LastActive = DateTime.UtcNow;
			IsActive = true;
			await SmtpStream.WriteLineAsync("MAIL FROM: <" +
										(mailFrom == null ? string.Empty : mailFrom.Address) + ">" +
										(_DataTransportMime == SmtpTransportMIME._8BitUTF ? " BODY=8BITMIME" : string.Empty));
			string response = await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;
			IsActive = false;

			if (!response.StartsWith("250"))
				failedCallback(response);

			return true;
		}

		/// <summary>
		/// Send the RCPT TO command to the server using <paramref name="rcptTo"/> as parameter.
		/// </summary>
		/// <param name="rcptTo">Email address to use as parameter.</param>
		/// <param name="failedCallback">Action to call if command fails.</param>
		public async Task<bool> ExecRcptToAsync(MailAddress rcptTo, Action<string> failedCallback)
		{
			if (!base.Connected)
				return false;
			
			IsActive = true;
			_LastActive = DateTime.UtcNow;
			await SmtpStream.WriteLineAsync("RCPT TO: <" + rcptTo.Address + ">");
			string response = await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;
			IsActive = false;

			if (!response.StartsWith("250"))
				failedCallback(response);

			return true;
		}

		/// <summary>
		/// Send the data to the server
		/// </summary>
		/// <param name="data">Data to send to the server</param>
		/// <param name="failedCallback">Action to call if fails to send.</param>
		public async Task<bool> ExecDataAsync(string data, Action<string> failedCallback)
		{
			if (!base.Connected)
				return false;

			_LastActive = DateTime.UtcNow;
			IsActive = true;

			await SmtpStream.WriteLineAsync("DATA");
			string response = await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;

			if (!response.StartsWith("354"))
			{
				failedCallback(response);
				IsActive = false;
				return false;
			}

			// Increment the data commands as server has responded positiely.
			_DataCommands++;

			// Send the message data using the correct transport MIME
			SmtpStream.SetSmtpTransportMIME(_DataTransportMime);
			await SmtpStream.WriteAsync(data, false);
			await SmtpStream.WriteAsync(MtaParameters.NewLine + "." + MtaParameters.NewLine, false);
			_LastActive = DateTime.UtcNow;

			// Data done so return to 7-Bit mode.
			SmtpStream.SetSmtpTransportMIME(SmtpTransportMIME._7BitASCII);


			response = await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;
			IsActive = false;


			if (!response.StartsWith("250"))
				failedCallback(response);


			// If max messages have been sent quit the connection.			
			if (_DataCommands >= OutboundRules.OutboundRuleManager.GetMaxMessagesPerConnection(MXRecord, MtaIpAddress))
				Task.Run(() => ExecQuitAsync()).Wait();

			return true;
		}

		/// <summary>
		/// Send the SMTP Quit command to the Server.
		/// </summary>
		public async Task<bool> ExecQuitAsync()
		{
			if (!base.Connected)
				return false;

			IsActive = true;
			await SmtpStream.WriteLineAsync("QUIT");
			// Don't read response as don't care.
			// Close the TCP connection.
			base.GetStream().Close();
			base.Close();
			IsActive = false;
			return true;
		}

		/// <summary>
		/// Send the RSET command to the server.
		/// </summary>
		public async Task<bool> ExecRsetAsync()
		{
			if (!base.Connected)
			{
				Logging.Debug("Cannot RSET connection has been closed.");
				throw new Exception();
			}
			IsActive = true;
			await SmtpStream.WriteLineAsync("RSET");
			await SmtpStream.ReadAllLinesAsync();
			_LastActive = DateTime.UtcNow;
			IsActive = false;

			return true;
		}

		/// <summary>
		/// HELO or RSET depending on previous commands.
		/// </summary>
		/// <param name="failedCallback"></param>
		public async Task<bool> ExecHeloOrRsetAsync(Action<string> failedCallback)
		{
			if (!_HasHelloed)
				await ExecHeloAsync(failedCallback);
			else
				await ExecRsetAsync();

			return true;
		}
	}
}
