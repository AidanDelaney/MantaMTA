using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Transactions;
using MantaMTA.Core.Server;
using MantaMTA.Core.Smtp;
using NUnit.Framework;
	

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class ServerTests : TestFixtureBase
    {
		/// <summary>
		/// Test that we can create a server and connect to it.
		/// </summary>
		[Test]
		public void CreateServer()
		{
			using (SmtpServer s = new SmtpServer(8000))
			{
				TcpClient client = new TcpClient();
				client.Connect(IPAddress.Parse("127.0.0.1"), 8000);
				client.Close();
			}
		}

		/// <summary>
		/// Test that a message is received.
		/// </summary>
		[Test]
		public void SendMessage()
		{
			using (TransactionScope ts = CreateTransactionScopeObject())
			{
				using (SmtpServer s = new SmtpServer(8000))
				{
					TcpClient client = new TcpClient();
					client.Connect(IPAddress.Parse("127.0.0.1"), 8000);
					SmtpStreamHandler smtp = new SmtpStreamHandler(client);

					Action<string, string> sendLine = new Action<string,string>(delegate(string cmd, string expectedResponse)
					{
						smtp.WriteLine(cmd, false);
						string response = smtp.ReadLineAsync(false).Result;
						Console.WriteLine(cmd + " " + expectedResponse + " " + response);
						Assert.AreEqual(expectedResponse, response.Substring(0, 3));
					});

					string result = smtp.ReadLineAsync().Result;
					sendLine("HELO localhost", "250");
					sendLine("MAIL FROM: <local@localhost>", "250");
					sendLine("RCPT TO: <postmaster@localhost>", "250");
					sendLine("DATA", "354");
					smtp.WriteLine("Hello", false);
					sendLine(".", "250");
					smtp.WriteLine("QUIT");
				}
			}
		}

		/// <summary>
		/// Tests that a message is queued for relaying to somewhere else
		/// </summary>
		[Test]
		public void QueueMessage()
		{
			using (TransactionScope ts = CreateTransactionScopeObject())
			{
				using (SmtpServer s = new SmtpServer(8000))
				{
					TcpClient client = new TcpClient();
					client.Connect(IPAddress.Parse("127.0.0.1"), 8000);
					SmtpStreamHandler smtp = new SmtpStreamHandler(client);

					Func<string, string, Task<bool>> sendLine = new Func<string, string, Task<bool>>(async delegate(string cmd, string expectedResponse)
					{
						await smtp.WriteLineAsync(cmd, false);
						string response = await smtp.ReadLineAsync(false);
						Console.WriteLine(cmd + " " + expectedResponse + " " + response);
						Assert.AreEqual(expectedResponse, response.Substring(0, 3));
						return true;
					});

					Task.Run(new Action(async delegate()
					{
						await smtp.ReadLineAsync();
						await sendLine("HELO localhost", "250");
						await sendLine("MAIL FROM: <local@localhost>", "250");
						await sendLine("RCPT TO: <daniel.longworth@colony101.co.uk>", "250");
						await sendLine("DATA", "354");
						await smtp.WriteLineAsync("Hello", false);
						await sendLine(".", "250");
						await smtp.WriteLineAsync("QUIT");
					})).Wait();
				}
			}
		}
    }
}
