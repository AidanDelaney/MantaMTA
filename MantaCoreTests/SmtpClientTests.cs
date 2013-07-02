using System;
using System.Net;
using System.Threading.Tasks;
using MantaMTA.Core.Server;
using MantaMTA.Core.Smtp;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
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
				MtaIpAddress.MtaIpAddress ipAddress = new MtaIpAddress.MtaIpAddress() { IPAddress = IPAddress.Parse("127.0.0.1") };
				MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(ipAddress);
				smtpClient.Connect(mxRecord);
				MantaMTA.Core.Smtp.SmtpClientPool.Enqueue(smtpClient);
				smtpClient = MantaMTA.Core.Smtp.SmtpClientPool.Dequeue(ipAddress, new MantaMTA.Core.DNS.MXRecord[] { mxRecord }, new Action<string>(delegate(string str) { }));

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
				MtaIpAddress.MtaIpAddress ipAddress = new MtaIpAddress.MtaIpAddress() { IPAddress = IPAddress.Parse("127.0.0.1") };
				MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(ipAddress);
				smtpClient.Connect(mxRecord);
				Assert.IsTrue(smtpClient.Connected);

				Action sendMessage = new Action(delegate()
				{
					Action<string> callback = new Action<string>(delegate(string str) { });
					Task.Run(async delegate()
					{
						await smtpClient.ExecHeloOrRsetAsync(callback);
						await smtpClient.ExecMailFromAsync(new System.Net.Mail.MailAddress("testing@localhost"), callback);
						await smtpClient.ExecRcptToAsync(new System.Net.Mail.MailAddress("testing@localhost"), callback);
						await smtpClient.ExecDataAsync("hello", callback);
						return true;
					}).Wait();
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
				MtaIpAddress.MtaIpAddress outboundEndpoint = new MtaIpAddress.MtaIpAddress() { IPAddress = IPAddress.Parse("127.0.0.1") };
				MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(outboundEndpoint);
				smtpClient.Connect(mxRecord);

				Assert.IsTrue(smtpClient.Connected);
				System.Threading.Thread.Sleep((MantaMTA.Core.MtaParameters.Client.ConnectionIdleTimeoutInterval + 5) * 1000);
				Assert.IsFalse(smtpClient.Connected);
			}
		}
	}
}
