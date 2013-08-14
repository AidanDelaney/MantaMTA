using System;
using MantaMTA.Core.Events;
using NUnit.Framework;

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
	}
}
