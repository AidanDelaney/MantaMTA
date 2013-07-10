using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MantaMTA.Core.Events;

namespace MantaCoreEventsTests_NET4_0
{
	[TestFixture]
	public class MantaCoreEventsTest
	{
		/// <summary>
		/// Check the ConvertSmtpCodeToMantaBounceCode() method is working correctly.
		/// </summary>
		[Test]
		public void SmtpCodeToMantaBounceCode()
		{
			// Check some random SMTP codes are converted correctly.
			Assert.AreEqual(MantaBounceCode.NotABounce, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(200));
			Assert.AreEqual(MantaBounceCode.DeferredUnableToConnect, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(421));
			Assert.AreEqual(MantaBounceCode.DeferredBadEmailAddress, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(450));
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(550));


			// Check some unknown/made-up SMTP codes are converted to appropriate values based on the first digit
			// (so 4xx = a temporary issue and 5xx is a permanent issue).
			Assert.AreEqual(MantaBounceCode.Unknown, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(199));
			Assert.AreEqual(MantaBounceCode.NotABounce, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(299));
			Assert.AreEqual(MantaBounceCode.Unknown, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(399));
			Assert.AreEqual(MantaBounceCode.DeferredGeneral, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(499));
			Assert.AreEqual(MantaBounceCode.RejectedGeneral, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(599));
		}


		/// <summary>
		/// Check the ConvertNdrCodeToMantaBounceCode() method is working correctly.
		/// </summary>
		[Test]
		public void NdrCodeToMantaBounceCode()
		{
			// "X.1.5" is "Destination mailbox address valid" indicating _successfull_ delivery.
			Assert.AreEqual(MantaBounceCode.NotABounce, BounceRulesManager.Instance.ConvertNdrCodeToMantaBounceCode("4.1.5"));
			Assert.AreEqual(MantaBounceCode.NotABounce, BounceRulesManager.Instance.ConvertNdrCodeToMantaBounceCode("5.1.5"));


			// Check a few error codes are interpreted correctly (2.x.x are success, 4.x.x are temporary errors, 5.x.x are permanent errors).
			Assert.AreEqual(MantaBounceCode.NotABounce, BounceRulesManager.Instance.ConvertNdrCodeToMantaBounceCode("2.1.1"));
			Assert.AreEqual(MantaBounceCode.DeferredBadEmailAddress, BounceRulesManager.Instance.ConvertNdrCodeToMantaBounceCode("4.1.1"));
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, BounceRulesManager.Instance.ConvertNdrCodeToMantaBounceCode("5.1.1"));
		}


		/// <summary>
		/// Check that bounce content is identified correctly.
		/// </summary>
		[Test]
		public void BounceProcessing()
		{
			MantaBounceType bounceType = MantaBounceType.Unknown;
			MantaBounceCode bounceCode = MantaBounceCode.Unknown;
			string bounceMessage = string.Empty;
			bool returned = false;

			//Assert.AreEqual(EmailProcessingResult.SuccessNoAction, EventsManager.Instance.ProcessBounceEmail(string.Empty));

			//Assert.AreEqual(EmailProcessingResult.SuccessNoAction, EventsManager.Instance.ProcessBounceEmail(""));

			returned = EventsManager.Instance.ParseBounceMessage(, out bounceType, out bounceCode, out bounceMessage);

			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, bounceType);
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, bounceCode);
		}


		/// <summary>
		/// Check Non-Delivery Reports are processed correctly.
		/// </summary>
		[Test]
		public void NdrBounceProcessing()
		{
			MantaBounceType bounceType = MantaBounceType.Unknown;
			MantaBounceCode bounceCode = MantaBounceCode.Unknown;
			string bounceMessage = string.Empty;


			bool returned = EventsManager.Instance.ParseNdr(@"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Tue, 9 Oct 2012 19:02:10 +0100

Final-Recipient: rfc822;some.user@colony101.co.uk
Action: failed
Status: 5.1.1
Diagnostic-Code: smtp;550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", out bounceType, out bounceCode, out bounceMessage);
			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, bounceType);
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, bounceCode);
			Assert.AreEqual(@"550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", bounceMessage);
		}


		[Test]
		public void SmtpResponseBounceProcessing()
		{
			MantaBounceEvent mbEvent = null;

			mbEvent = EventsManager.Instance.ProcessSmtpResponseMessage(@"550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", "bobobobobobobobobobobob@aol.com", 100);
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, mbEvent.BounceCode);
			Assert.AreEqual(MantaBounceType.Hard, mbEvent.BounceType);
			Assert.AreEqual("bobobobobobobobobobobob@aol.com", mbEvent.EmailAddress);
			Assert.AreEqual(MantaEventType.Bounce, mbEvent.EventType);
			Assert.AreEqual("5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", mbEvent.Message);
			// Need to get GetSendIdFromInternalSendId() method wired up before we can predict what this will give us.
			// Likely to be the current DateTime as a string if we provide an InternalSendID value that doesn't yet exist.
			// Assert.AreEqual("100", mbEvent.SendID);

			
			// "550-5.1.1 The email account that you tried to reach does not exist. Please try
			//550-5.1.1 double-checking the recipient's email address for typos or
			//550-5.1.1 unnecessary spaces. Learn more at
			//550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp"

			// "550 Requested action not taken: mailbox unavailable"


			//EventsManager.Instance.ProcessSmtpResponseMessage("", )
		}
	}
}
