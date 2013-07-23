using System.IO;
using System.Net.Mail;
using MantaMTA.Core.Message;
using MantaMTA.Core.Server;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class InboundEmails : TestFixtureBase
	{
		/// <summary>
		/// Tests that abuse emails are received by the server.
		/// </summary>
		[Test]
		public void Abuse()
		{
			TestMailbox(MtaParameters.AbuseDropFolder, "abuse@localhost");
		}

		/// <summary>
		/// Tests that the portmaster@ emails are recieved by the server.
		/// </summary>
		[Test]
		public void Postmaster()
		{
			TestMailbox(MtaParameters.PostmasterDropFolder, "postmaster@localhost");
		}

		/// <summary>
		/// Test that email bounces are received by the server.
		/// </summary>
		[Test]
		public void Bounce()
		{
			string returnpath = ReturnPathManager.GenerateReturnPath("testing@localhost", 1);
			TestMailbox(MtaParameters.BounceDropFolder, returnpath);
		}

		/// <summary>
		/// Tests that feedback emails are received by the server.
		/// </summary>
		[Test]
		public void FeedbackLoop()
		{
			TestMailbox(MtaParameters.FeedbackLoopDropFolder, "fbl@localhost");
		}

		/// <summary>
		/// Tests that any other inbound emails bounce with 550.
		/// </summary>
		[Test]
		public void Unknown()
		{
			bool failedRecipient = false;
			try
			{
				TestMailbox(MtaParameters.MTA_DROPFOLDER, "unknown@localhost");
			}
			catch (SmtpFailedRecipientException)
			{
				failedRecipient = true;
			}

			Assert.AreEqual(true, failedRecipient);
		}

		/// <summary>
		/// Tests a mailbox by attempting to send an email to it.
		/// </summary>
		/// <param name="dropFolder">Directory where we expect emails to go.</param>
		/// <param name="rcptTo">Who we are sending to.</param>
		private void TestMailbox(string dropFolder, string rcptTo)
		{
			// Delete the drop folder and any existing emails in it.
			if (Directory.Exists(dropFolder))
				Directory.Delete(dropFolder, true);

			using (CreateTransactionScopeObject())
			{
				// Create the SMTP server.
				using (SmtpServer s = new SmtpServer(25))
				{
					// Create a client to the sever.
					SmtpClient client = new SmtpClient("localhost", 25);
					try
					{
						// Try and send the email.
						client.Send("testing@localhost", rcptTo, "", "");
					}
					catch (SmtpFailedRecipientException)
					{
						// If mailbox unknown received catch and rethrow exception.
						throw;
					}

					// There should only be one email in the drop folder.
					int emlCount = new DirectoryInfo(dropFolder).GetFiles().Length;
					Assert.AreEqual(1, emlCount);
				}
			}
		}
	}
}
