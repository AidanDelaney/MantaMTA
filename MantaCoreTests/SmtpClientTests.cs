using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
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
				VirtualMta.VirtualMTA ipAddress = new VirtualMta.VirtualMTA() { IPAddress = IPAddress.Parse("127.0.0.1") };
				MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue, DNS.MxRecordSrc.A);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(ipAddress);
				smtpClient.ConnectAsync(mxRecord).Wait();
				MantaMTA.Core.Smtp.SmtpClientPool.Instance.Enqueue(smtpClient);
				smtpClient = MantaMTA.Core.Smtp.SmtpClientPool.Instance.DequeueAsync(ipAddress, new MantaMTA.Core.DNS.MXRecord[] { mxRecord }).Result.SmtpOutboundClient;

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
				VirtualMta.VirtualMTA ipAddress = new VirtualMta.VirtualMTA() { IPAddress = IPAddress.Parse("127.0.0.1") };
				MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue, DNS.MxRecordSrc.A);

				SmtpOutboundClient smtpClient = new SmtpOutboundClient(ipAddress);
				smtpClient.ConnectAsync(mxRecord).Wait();
				Assert.IsTrue(smtpClient.Connected);

				Action sendMessage = new Action(delegate()
				{
					Action<string> callback = new Action<string>(delegate(string str) { });
					Task.Run(async delegate()
					{
						await smtpClient.ExecHeloOrRsetAsync(callback);
						await smtpClient.ExecMailFromAsync(new System.Net.Mail.MailAddress("testing@localhost"), callback);
						await smtpClient.ExecRcptToAsync(new System.Net.Mail.MailAddress("postmaster@localhost"), callback);
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
			if(MessageBox.Show("Do you want to run the idle timeout test?", "Idle timeout test", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				using (SmtpServer s = new SmtpServer(25))
				{
					VirtualMta.VirtualMTA outboundEndpoint = new VirtualMta.VirtualMTA() { IPAddress = IPAddress.Parse("127.0.0.1") };
					MantaMTA.Core.DNS.MXRecord mxRecord = new MantaMTA.Core.DNS.MXRecord("localhost", 10, uint.MaxValue, DNS.MxRecordSrc.A);

					SmtpOutboundClient smtpClient = new SmtpOutboundClient(outboundEndpoint);
					smtpClient.ConnectAsync(mxRecord).Wait();

					Assert.IsTrue(smtpClient.Connected);
					System.Threading.Thread.Sleep((MantaMTA.Core.MtaParameters.Client.ConnectionIdleTimeoutInterval + 5) * 1000);
					Assert.IsFalse(smtpClient.Connected);
				}
			}
		}
	}
}
