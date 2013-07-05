using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MantaMTA.Core.Events;

namespace MantaCoreEventsTests_.NET4._0_
{
	[TestFixture]
	public class MantaCoreEventsTest
	{
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
			Assert.AreEqual(MantaBounceCode.DeferredGeneral, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(499));
			Assert.AreEqual(MantaBounceCode.RejectedUnknown, BounceRulesManager.Instance.ConvertSmtpCodeToMantaBounceCode(599));
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

			returned = EventsManager.Instance.ParseBounceMessage(@"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Tue, 9 Oct 2012 19:02:10 +0100

Final-Recipient: rfc822;some.user@colony101.co.uk
Action: failed
Status: 5.1.1
Diagnostic-Code: smtp;550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", out bounceType, out bounceCode, out bounceMessage);

			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, bounceType);
			Assert.AreEqual(MantaBounceCode.RejectedBadEmailAddress, bounceCode);

		}


		[Test]
		public void NdrBounceProcessing()
		{
			MantaBounceType bounceType = MantaBounceType.Unknown;
			MantaBounceCode bounceCode = MantaBounceCode.Unknown;
			string bounceMessage = string.Empty;


			bool returned = EventsManager.Instance.ParseNdr(@"", out bounceType, out bounceCode, out bounceMessage);
			Assert.AreEqual(false, returned);
			Assert.AreEqual(MantaBounceType.Unknown, bounceType);
			Assert.AreEqual(MantaBounceCode.Unknown, bounceCode);
			Assert.AreEqual(string.Empty, bounceMessage);

		}
	}
}
