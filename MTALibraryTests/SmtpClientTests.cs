using System;
using System.Net;
using Colony101.MTA.Library.Server;
using Colony101.MTA.Library.Smtp;
using NUnit.Framework;

namespace MTALibraryTests
{
	[TestFixture]
	public class SmtpClientTests
	{
		/// <summary>
		/// Ensure that SMTP clients can be enqueued and dequeued.
		/// </summary>
		[Test]
		public void SmtpClientPoolTest()
		{
			using (SmtpServer s = new SmtpServer(25))
			{
				IPEndPoint outboundEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
				Colony101.MTA.Library.DNS.MXRecord mxRecord = new Colony101.MTA.Library.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(outboundEndpoint);
				smtpClient.Connect(mxRecord);
				Colony101.MTA.Library.Smtp.SmtpClientPool.Enqueue(smtpClient);
				Colony101.MTA.Library.Smtp.SmtpClientPool.TryDequeue(outboundEndpoint, new Colony101.MTA.Library.DNS.MXRecord[] { mxRecord }, new Action<string>(delegate(string str) { }), out smtpClient);

				Assert.NotNull(smtpClient);
				Assert.IsTrue(smtpClient.Connected);
			}
		}

		/// <summary>
		/// Test to ensure that the max messages cannot be exceeded.
		/// </summary>
		[Test]
		public void SmtpClientMaxMessages()
		{
			using (SmtpServer s = new SmtpServer(25))
			{
				IPEndPoint outboundEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
				Colony101.MTA.Library.DNS.MXRecord mxRecord = new Colony101.MTA.Library.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(outboundEndpoint);
				smtpClient.Connect(mxRecord);
				Assert.IsTrue(smtpClient.Connected);

				Action sendMessage = new Action(delegate()
				{
					Action<string> callback = new Action<string>(delegate(string str) { });
					smtpClient.ExecHeloOrRset(callback);
					smtpClient.ExecMailFrom(new System.Net.Mail.MailAddress("testing@localhost"), callback);
					smtpClient.ExecRcptTo(new System.Net.Mail.MailAddress("testing@localhost"), callback);
					smtpClient.ExecData("hello", callback);
				});

				sendMessage();
				Assert.IsTrue(smtpClient.Connected);

				sendMessage();
				Assert.IsTrue(smtpClient.Connected);

				sendMessage();
				Assert.IsTrue(smtpClient.Connected);

				sendMessage();
				Assert.IsTrue(smtpClient.Connected);

				sendMessage();
				Assert.IsFalse(smtpClient.Connected);
			}
		}

		/// <summary>
		/// Test that the SmtpClient idle timeout is being fired and disconnected.
		/// </summary>
		[Test]
		public void SmtpClientIdleTimeout()
		{
			using (SmtpServer s = new SmtpServer(25))
			{
				IPEndPoint outboundEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
				Colony101.MTA.Library.DNS.MXRecord mxRecord = new Colony101.MTA.Library.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(outboundEndpoint);
				smtpClient.Connect(mxRecord);

				Assert.IsTrue(smtpClient.Connected);
				System.Threading.Thread.Sleep((Colony101.MTA.Library.MtaParameters.Client.CONNECTION_IDLE_TIMEOUT_INTERVAL + 5) * 1000);
				Assert.IsFalse(smtpClient.Connected);
			}
		}
	}
}
