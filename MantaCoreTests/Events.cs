using System;
using MantaMTA.Core.Events;
using NUnit.Framework;
using MantaMTA.Core.Message;
using System.IO;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class Events : TestFixtureBase
	{
		/// <summary>
		/// Test ensures we can save a MantaBounceEvent to the database and get it back.
		/// </summary>
		[Test]
		public void SaveAndGetBounce()
		{
			using (CreateTransactionScopeObject())
			{
				MantaBounceEvent originalEvt = new MantaBounceEvent
				{
					BounceInfo = new BouncePair
					{
						BounceCode = MantaBounceCode.BadEmailAddress,
						BounceType = MantaBounceType.Hard
					},
					EmailAddress = "some.user@colony101.co.uk",
					EventTime = DateTime.UtcNow,
					EventType = MantaEventType.Bounce,
					Message = "550 Invalid Inbox",
					SendID = "qwerty"
				};

				originalEvt.ID = EventsManager.Instance.Save(originalEvt);

				MantaBounceEvent savedEvt = (MantaBounceEvent)EventsManager.Instance.GetEvent(originalEvt.ID);

				Assert.NotNull(savedEvt);
				Assert.AreEqual(originalEvt.BounceInfo.BounceCode, savedEvt.BounceInfo.BounceCode);
				Assert.AreEqual(originalEvt.BounceInfo.BounceType, savedEvt.BounceInfo.BounceType);
				Assert.AreEqual(originalEvt.EmailAddress, savedEvt.EmailAddress);
				Assert.That(savedEvt.EventTime, Is.EqualTo(originalEvt.EventTime).Within(TimeSpan.FromSeconds(1)));
				Assert.AreEqual(originalEvt.EventType, savedEvt.EventType);
				Assert.AreEqual(originalEvt.ID, savedEvt.ID);
				Assert.AreEqual(originalEvt.Message, savedEvt.Message);
				Assert.AreEqual(originalEvt.SendID, savedEvt.SendID);
			}
		}

		/// <summary>
		/// Test ensures we can save a MantaAbuseEvent to the database and get it back.
		/// </summary>
		[Test]
		public void SaveAndGetAbuse()
		{
			using (CreateTransactionScopeObject())
			{
				MantaAbuseEvent origAbuse = new MantaAbuseEvent
				{
					EmailAddress = "some.user@colony101.co.uk",
					EventTime = DateTime.UtcNow,
					EventType = MantaEventType.Abuse,
					SendID = "qwerty"
				};

				origAbuse.ID = EventsManager.Instance.Save(origAbuse);
				MantaAbuseEvent savedAbuse = (MantaAbuseEvent)EventsManager.Instance.GetEvent(origAbuse.ID);
				Assert.NotNull(savedAbuse);
				Assert.AreEqual(origAbuse.EmailAddress, savedAbuse.EmailAddress);
				Assert.That(savedAbuse.EventTime, Is.EqualTo(origAbuse.EventTime).Within(TimeSpan.FromSeconds(1)));
				Assert.AreEqual(origAbuse.EventType, savedAbuse.EventType);
				Assert.AreEqual(origAbuse.ID, savedAbuse.ID);
				Assert.AreEqual(origAbuse.SendID, savedAbuse.SendID);
			}
		}

		[Test]
		public void SmtpResponseTest()
		{
			using (CreateTransactionScopeObject())
			{
				bool result = EventsManager.Instance.ProcessSmtpResponseMessage("550 User Unknown", "some.user@colony101.co.uk", 1);
				Assert.IsTrue(result);

				MantaEventCollection events = EventsManager.Instance.GetEvents();
				Assert.AreEqual(1, events.Count);
				Assert.IsTrue(events[0] is MantaBounceEvent);
				MantaBounceEvent bounce = (MantaBounceEvent)events[0];
				Assert.AreEqual(MantaBounceCode.BadEmailAddress, bounce.BounceInfo.BounceCode);
				Assert.AreEqual(MantaBounceType.Hard, bounce.BounceInfo.BounceType);
				Assert.AreEqual("some.user@colony101.co.uk", bounce.EmailAddress);
				Assert.AreEqual("550 User Unknown", bounce.Message);
				Assert.AreEqual("TestData", bounce.SendID);
			}
		}


		/// <summary>
		/// Full checking of processing an SMTP response message.
		/// </summary>
		[Test]
		public void SmtpResponseBounceProcessing()
		{
			using (CreateTransactionScopeObject())
			{
				bool result = false;


				// Check an AOL response.
				result = EventsManager.Instance.ProcessSmtpResponseMessage(@"550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", "bobobobobobobobobobobob@aol.com", 1);
				Assert.IsTrue(result);


				// Check a GMail response (multi-line).
				result = EventsManager.Instance.ProcessSmtpResponseMessage(@"550-5.1.1 The email account that you tried to reach does not exist. Please try
550-5.1.1 double-checking the recipient's email address for typos or
550-5.1.1 unnecessary spaces. Learn more at
550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp", "bobobobobobobobobobobob@gmail.com", 1);
				Assert.IsTrue(result);
			}
		}


		[Test]
		public void FeedbackLoop()
		{
			using (CreateTransactionScopeObject())
			{
				EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.AolAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
				MantaEventCollection events = EventsManager.Instance.GetEvents();
				Assert.AreEqual(1, events.Count);
				Assert.IsTrue(events[0] is MantaAbuseEvent);
				MantaAbuseEvent abuse = (MantaAbuseEvent)events[0];
				Assert.AreEqual("test@remote", abuse.EmailAddress);
				Assert.AreEqual("TestData", abuse.SendID);


				result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.YahooAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
				events = EventsManager.Instance.GetEvents();
				Assert.AreEqual(2, events.Count);
				Assert.IsTrue(events[1] is MantaAbuseEvent);
				abuse = (MantaAbuseEvent)events[1];
				Assert.AreEqual("some.user@yahoo.co.uk", abuse.EmailAddress);
				Assert.AreEqual("TestData", abuse.SendID);

				result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.HotmailAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
				events = EventsManager.Instance.GetEvents();
				Assert.AreEqual(3, events.Count);
				Assert.IsTrue(events[2] is MantaAbuseEvent);
				abuse = (MantaAbuseEvent)events[2];
				Assert.AreEqual("some.user@hotmail.com", abuse.EmailAddress);
				Assert.AreEqual("TestData", abuse.SendID);
			}
		}


		[Test]
		public void FindBodyPartsByType()
		{
			BodyPart[] bodyParts;
			BodyPart foundBodyPart;


			// Find a Delivery Report.
			bodyParts = MimeMessage.Parse(System.IO.File.OpenText(@".\..\..\Many BodyParts - structure.eml").ReadToEnd()).BodyParts;
			Assert.IsTrue(EventsManager.Instance.FindFirstBodyPartByMediaType(bodyParts, "message/delivery-status", out foundBodyPart));
			Assert.AreEqual(@"Reporting-MTA: dns;someserver.com
Received-From-MTA: dns;mail.someserver.com
Arrival-Date: Fri, 12 Jul 2013 10:09:28 +0000

Original-Recipient: rfc822;someone@someserver.com
Final-Recipient: rfc822;finalsomeone@someserver.com
Action: failed
Status: 5.2.2
Diagnostic-Code: smtp;554-5.2.2 mailbox full

", foundBodyPart.GetDecodedBody());




			// Find some abuse reports.

			// AOL use abuse reports (though all the values are redacted).
			bodyParts = MimeMessage.Parse(FeedbackLoopEmails.AolAbuse).BodyParts;
			Assert.IsTrue(EventsManager.Instance.FindFirstBodyPartByMediaType(bodyParts, "message/feedback-report", out foundBodyPart));
			Assert.AreEqual(@"Feedback-Type: abuse
User-Agent: AOL SComp
Version: 0.1
Received-Date: Mon,  1 Jul 2013 14:37:29 -0400 (EDT)
Source-IP: 5.79.26.23
Reported-Domain: snt3.net
Redacted-Address: redacted
Redacted-Address: redacted@

", foundBodyPart.GetDecodedBody());

			// Yahoo! use abuse reports.
			bodyParts = MimeMessage.Parse(FeedbackLoopEmails.YahooAbuse).BodyParts;
			Assert.IsTrue(EventsManager.Instance.FindFirstBodyPartByMediaType(bodyParts, "message/feedback-report", out foundBodyPart));
			Assert.AreEqual(@"Feedback-Type: abuse
User-Agent: Yahoo!-Mail-Feedback/1.0
Version: 0.1
Original-Mail-From: <return-some.user=yahoo.co.uk-1@colony101.co.uk>
Original-Rcpt-To: some.user@yahoo.co.uk
Received-Date: Sun, 24 Feb 2013 13:44:08 PST
Reported-Domain: colony101.co.uk
Authentication-Results: mta1126.mail.ir2.yahoo.com  from=colony101.co.uk; domainkeys=neutral (no sig);  from=colony101.co.uk; dkim=pass (ok)
", foundBodyPart.GetDecodedBody());

			// Hotmail's abuse emails don't contain a message/feedback-report BodyPart.
			bodyParts = MimeMessage.Parse(FeedbackLoopEmails.HotmailAbuse).BodyParts;
			Assert.IsFalse(EventsManager.Instance.FindFirstBodyPartByMediaType(bodyParts, "message/feedback-report", out foundBodyPart));
			Assert.IsNull(foundBodyPart);
		}
	}
}
