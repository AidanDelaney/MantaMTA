using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Threading;

namespace Colony101.MTA.Library.Server
{
	public class SmtpServer
	{
		/// <summary>
		/// When set to the true, the server has been requested to stop.
		/// </summary>
		private bool _StopRequested = false;
		/// <summary>
		/// Listens to TCP socket.
		/// </summary>
		private TcpListener _TcpListener = null;
		/// <summary>
		/// Thread for the server to run on.
		/// </summary>
		private Thread _ServerThread = null;

		/// <summary>
		/// Make default constructor private so code from other classes has to use SmtpServer(port)
		/// </summary>
		private SmtpServer()
		{
			throw new Exception("Must specify port for SMTP Server.");
		}

		/// <summary>
		/// Creates an instance if the SMTP Server.
		/// </summary>
		/// <param name="port">Port number that server bind to.</param>
		public SmtpServer(int port)
		{
			// Create the TCP Listener using specified port on all IPs
			_TcpListener = new TcpListener(IPAddress.Any, port);
			_ServerThread = new Thread(
				new ThreadStart(delegate()
				{
					_TcpListener.Start();
					while (!_StopRequested)
					{
						// AcceptTcpClient will block until done.
						TcpClient client = _TcpListener.AcceptTcpClient();

						// When client connects create new thread and call handler.
						// DanL : This should use a threadpool or server could easily get overloaded.
						Thread clientThread = new Thread(new ParameterizedThreadStart(HandleSmtpConnection));
						clientThread.Start(client);
					}
				}));
			_ServerThread.Start();
		}

		/// <summary>
		/// Stops the SMTP Server.
		/// </summary>
		public void Stop()
		{
			_TcpListener.Stop();
			_StopRequested = true;
		}

		/// <summary>
		/// Method handles a single connection from a client.
		/// </summary>
		/// <param name="client">Connection with the client.</param>
		private void HandleSmtpConnection(object obj)
		{
			TcpClient client = (TcpClient)obj;
			SmtpStreamHandler smtpStream = new SmtpStreamHandler(client);			

			// Identify our MTA
			smtpStream.WriteLine("220 " + GetServerHostname(client) + " SMTP " + MtaParameters.MTA_NAME + " Ready");

			// Set to true when the client has sent quit command.
			bool quit = false;

			// Set to true when the client has said hello.
			bool hasHello = false;

			// Hostname of the client as it self identified in the HELO.
			string heloHost = string.Empty;

			SmtpTransaction mailTransaction = null;

			// As long as the client is connected and hasn't sent the quit command then keep accepting commands.
			while (client.Connected && !quit)
			{
				// Read the next command. If no line then this will wait for one.
				string cmd = smtpStream.ReadLine();

				// Client Disconnected.
				if (cmd == null)
					break;

				#region SMTP Commands that can be run before HELO is issued by client.

				// Handle the QUIT command. Should return 221 and close connection.
				if (cmd.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
				{
					quit = true;
					smtpStream.WriteLine("221 Goodbye");
					continue;
				}

				// Reset the mail transaction state. Forget any mail from rcpt to data.
				if (cmd.Equals("RSET", StringComparison.OrdinalIgnoreCase))
				{
					mailTransaction = null;
					smtpStream.WriteLine("250 Ok");
					continue;
				}

				// Do nothing except return 250. Do nothing just return success (250).
				if (cmd.Equals("NOOP", StringComparison.OrdinalIgnoreCase))
				{
					smtpStream.WriteLine("250 Ok");
					continue;
				}

				#endregion

				// EHLO should 500 Bad Command as we don't support enhanced services, clients should then HELO.
				// We need to get the hostname provided by the client as it will be used in the recivied header.
				if (cmd.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
				{
					// Helo should be followed by a hostname, if not syntax error.
					if(cmd.IndexOf(" ") < 0)
					{
						smtpStream.WriteLine("501 Syntax error");
						continue;
					}

					// Grab the hostname.
					heloHost = cmd.Substring(cmd.IndexOf(" ")).Trim();

					// There should not be any spaces in the hostname if it is sytax error.
					if(heloHost.IndexOf(" ") >= 0)
					{
						smtpStream.WriteLine("501 Syntax error");
						heloHost = string.Empty;
						continue;
					}

					// Client has now said hello so set connection variable to true and 250 back to the client.
					hasHello = true;
					smtpStream.WriteLine("250 Hello " + heloHost + "[" + smtpStream.RemoteAddress.ToString() + "]");
					continue;
				}

				#region Commands that must be after a HELO

				// Client MUST helo before being allowed to do any of these commands.
				if (!hasHello)
				{
					smtpStream.WriteLine("503 HELO first");
					continue;
				}

				// Mail From should have a valid email address parameter, if it doesn't system error.
				// Mail From should also begin new transaction and forget any previous mail from, rcpt to or data commands.
				// Do this by creating new instance of SmtpTransaction class.
				if (cmd.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
				{
					mailTransaction = new SmtpTransaction();
					string mailFrom = string.Empty;
					try
					{
						mailFrom = new System.Net.Mail.MailAddress(cmd.Substring(cmd.IndexOf(":") + 1)).Address;
					}
					catch (Exception)
					{
						// Mail from not valid email.
						smtpStream.WriteLine("501 Syntax error");
						continue;
					}

					// If we got this far mail from has an valid email address parameter so set it in the transaction
					// and return success to the client.
					mailTransaction.MailFrom = mailFrom;
					smtpStream.WriteLine("250 Ok");
					continue;
				}

				// RCPT TO should have an email address parameter. It can only be set if MAIL FROM has already been set,
				// multiple RCPT TO addresses can be added.
				if (cmd.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
				{
					// Check we have a Mail From address.
					if (mailTransaction == null ||
						string.IsNullOrWhiteSpace(mailTransaction.MailFrom))
					{
						smtpStream.WriteLine("503 Bad sequence of commands");
						continue;
					}

					// Check that the RCPT TO has a valid email address parameter.
					MailAddress rcptTo = null;
					try
					{
						rcptTo = new MailAddress(cmd.Substring(cmd.IndexOf(":") + 1));
					}
					catch (Exception)
					{
						// Mail from not valid email.
						smtpStream.WriteLine("501 Syntax error");
						continue;
					}


					// Check to see if mail is to be delivered locally or relayed for delivery somewhere else.
					if (!MtaParameters.LocalDomains.Contains(rcptTo.Host.ToLower()))
					{
						// Messages isn't for delivery on this server.
						// Check if we are allowed to relay for the client IP
						if (!MtaParameters.IPsToAllowRelaying.Contains(smtpStream.RemoteAddress.ToString()))
						{
							// This server cannot deliver or relay message for the MAIL FROM + RCPT TO addresses.
							// This should be treated as a permament failer, tell client not to retry.
							smtpStream.WriteLine("554 Cannot relay");
							continue;
						}

						// Message is for relaying.
						mailTransaction.MessageDestination = Enums.MessageDestination.Relay;
					}
					else
					{

						// Message to be delivered locally.
						mailTransaction.MessageDestination = Enums.MessageDestination.Self;
					}

					// Add the recipient.
					mailTransaction.RcptTo.Add(rcptTo.ToString());
					smtpStream.WriteLine("250 Ok");
					continue;
				}

				// Handle the data command, all commands from the client until single line with only '.' should be treated as
				// a single blob of data.
				if (cmd.Equals("DATA", StringComparison.OrdinalIgnoreCase))
				{
					// Must have a MAIL FROM before data.
					if (mailTransaction == null ||
						string.IsNullOrWhiteSpace(mailTransaction.MailFrom))
					{
						smtpStream.WriteLine("503 Bad sequence of commands");
						continue;
					}
					// Must have RCPT's before data.
					else if (mailTransaction.RcptTo.Count < 1)
					{
						smtpStream.WriteLine("554 No valid recipients");
						continue;
					}
					
					// Tell the client we are now accepting there data.
					smtpStream.WriteLine("354 Go ahead");

					// Wait for the first data line. Don't log data in SMTP log file.
					string dataline = smtpStream.ReadLine(false);
					// Loop until data client stops sending us data.
					while(!dataline.Equals("."))
					{
						// Add the line to existing data.
						mailTransaction.Data += dataline + Environment.NewLine;

						// Wait for the next data line. Don't log data in SMTP log file.
						dataline = smtpStream.ReadLine(false);
					}

					// Once data is finished we have mail for delivery or relaying.
					// Add the Received header.
					mailTransaction.SetHeaders(string.Format("Received: from {0}[{1}] by {2}[{3}] on {4}",
						heloHost,
						smtpStream.RemoteAddress.ToString(),
						GetServerHostname(client),
						smtpStream.LocalAddress.ToString(),
						DateTime.Now.ToString("ddd, dd MMM yyyy HH':'mm':'ss K")));
					mailTransaction.Save(smtpStream.LocalAddress.ToString());
					
					// Done with transaction, clear it and inform client message success and QUEUED
					mailTransaction = null;
					smtpStream.WriteLine("250 Message queued for delivery");
					continue;
				}

				#endregion


				// If got this far then we don't known the command.
				smtpStream.WriteLine("500 Unknown command");
			}

			// Client has issued QUIT command or connecion lost.
			client.Close();
		}
		
		/// <summary>
		/// Gets the hostname for the server that is being connected to by client.
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		private string GetServerHostname(TcpClient client)
		{
			string serverIPAddress = (client.Client.LocalEndPoint as IPEndPoint).Address.ToString();
			string serverHost = string.Empty;
			try
			{
				serverHost = Dns.GetHostEntry(serverIPAddress).HostName;
			}
			catch (Exception)
			{
				// Host doesn't have reverse DNS. Use IP Address.
				serverHost = serverIPAddress;
			}

			return serverHost;
		}
	}
}
