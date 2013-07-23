using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MantaMTA.Core.Events;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MantaCoreEventsTests_NET4_0
{
	[TestFixture]
	public class MantaCoreEventsTest
	{
		/// <summary>
		/// Func we can use to compare two BouncePair objects to make Assert testing simpler.
		///		BouncePair #1:	expected
		///		BouncePair #2:	actual
		///		bool:	return value; true if BouncePair #1 and #2 have the same values, else false.
		/// </summary>
		Func<BouncePair, BouncePair, bool> AreBouncePairsTheSame = new Func<BouncePair, BouncePair, bool>(delegate(BouncePair expected, BouncePair actual) { return (actual.BounceType == expected.BounceType && actual.BounceCode == expected.BounceCode); });


		/// <summary>
		/// Checks that the SmtpResponse Regex pattern is working as intended.
		/// </summary>
		/// <param name="msg">An example of an SMTP response message to be tested.</param>
		/// <returns>true if the SmtpResponse Regex pattern matches, else false.</returns>
		delegate bool CheckSmtpResponseRegexPattern(string msg, string expectedSmtpCode, string expectedNdrCode, string expectedDetail);


		/// <summary>
		/// Check the ConvertSmtpCodeToMantaBounceCode() method is working correctly.
		/// </summary>
		[Test]
		public void SmtpCodesToMantaBouncePairs()
		{
			// Check some common values.

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(450)
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair{ BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.UnableToConnect },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(421)
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(450)
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.BadEmailAddress },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(550)
				)
			);



			// Check some unknown/made-up SMTP codes are converted to appropriate values based on the first digit
			// (so 4xx = a temporary issue and 5xx is a permanent issue and others aren't bounces).
			// Note: we can't assume that anything prefixed with 4 or 5 will be a bounce though, just a temp or perm
			// issue.

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Unknown, BounceCode = MantaBounceCode.Unknown },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(199)
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Unknown, BounceCode = MantaBounceCode.NotABounce },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(299)
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Unknown, BounceCode = MantaBounceCode.NotABounce },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(399)
				)
			);

			// A temporary unknown issue.
			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.Unknown},
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(499)
				)
			);

			// A permanent unknown issue.
			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.Unknown },
					BounceRulesManager.Instance.ConvertSmtpCodeToMantaBouncePair(599)
				)
			);
		}


		/// <summary>
		/// Check the ConvertNdrCodeToMantaBounceCode() method is working correctly.
		/// </summary>
		[Test]
		public void NdrCodesToMantaBouncePairs()
		{
			// "X.1.5" is "Destination mailbox address valid" indicating _successful_ delivery.
			// Bit odd as "4.1.5" would surefly indicate a temporary successful delivery and "5.1.5" a permanent one.  :o>
	
			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.NotABounce },
					BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair("4.1.5")
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.NotABounce },
					BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair("5.1.5")
				)
			);


			// Check a few error codes are interpreted correctly (2.x.x are success, 4.x.x are temporary errors, 5.x.x are permanent errors).
			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Unknown, BounceCode = MantaBounceCode.NotABounce },
					BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair("2.1.1")
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress },
					BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair("4.1.1")
				)
			);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.BadEmailAddress },
					BounceRulesManager.Instance.ConvertNdrCodeToMantaBouncePair("5.1.1")
				)
			);
		}


		/// <summary>
		/// Check Non-Delivery Reports are processed correctly.
		/// </summary>
		[Test]
		public void NdrBounceProcessing()
		{
			BouncePair actualBouncePair;
			string bounceMessage;
			bool returned;

			// Provide a Diagnostic-Code field to be parsed.  This is preferred to the Status field as it should
			// contain more detail.
			returned = EventsManager.Instance.ParseNdr(@"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Tue, 9 Oct 2012 19:02:10 +0100

Final-Recipient: rfc822;some.user@colony101.co.uk
Action: failed
Status: 5.1.1
Diagnostic-Code: smtp;550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", out actualBouncePair, out bounceMessage);
			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, actualBouncePair.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, actualBouncePair.BounceCode);
			Assert.AreEqual(@"550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", bounceMessage);




			// Provide only a Status field to be parsed, no Diagnostic-Code.
			returned = EventsManager.Instance.ParseNdr(@"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Tue, 9 Oct 2012 19:02:10 +0100

Final-Recipient: rfc822;some.user@colony101.co.uk
Action: failed
Status: 5.1.1", out actualBouncePair, out bounceMessage);
			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, actualBouncePair.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, actualBouncePair.BounceCode);
			// 
			Assert.AreEqual("Status: 5.1.1", bounceMessage);
		}


		/// <summary>
		/// Check we're parsing bounce messages correctly.
		/// </summary>
		[Test]
		public void BounceMessageParsing()
		{
			BouncePair actualBouncePair;
			string bounceMessage;
			bool returned;


			#region AOL-specific Rule checks.

			// This message was taken from SNT1's SMTP logs.
			returned = EventsManager.Instance.ParseBounceMessage(@"421 4.7.1 : (DYN:T1) http://postmaster.info.aol.com/errors/421dynt1.html", out actualBouncePair, out bounceMessage);

			Assert.IsTrue(returned);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.RateLimitedByReceivingMta },
					actualBouncePair
				)
			);


			// This message is faked from the "DYN:T1" message above but with the code tweaked and don't know what the following
			// string is.
			returned = EventsManager.Instance.ParseBounceMessage(@"421 4.7.1 : (DYN:T2) some content", out actualBouncePair, out bounceMessage);

			Assert.IsTrue(returned);

			Assert.IsTrue(
				AreBouncePairsTheSame(
					new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.UnableToConnect },
					actualBouncePair
				)
			);

			#endregion AOL-specific Rule checks.



			// Some actual SMTP log entries from SNT1:
			//
			// 421 4.7.1 : (DYN:T1) http://postmaster.info.aol.com/errors/421dynt1.html
			// 421 4.7.1 Intrusion prevention active for [173.203.70.224][S]
			// 450 4.1.1 <mickludwell@jtc65.co.uk>: Recipient address rejected: unverified address: connect to mailgate.jtc65.co.uk[193.195.220.67]: Connection timed out
			// 450 4.1.1 <anthony.kneller@regens.co.uk>: Recipient address rejected: User unknown in virtual mailbox table
			// 450 4.2.0 <john@winweb.net>: Recipient address rejected: Greylisted
			// 451 Requested action aborted: local error in processing (code: 11)
			// 451 4.7.1 Access denied by DCC
			// 451 Internal resource temporarily unavailable - KBID10473 - http://www.mimecast.com/knowledgebase/kbid10473.htm#451
			// 451 Internal resource temporarily unavailable - http://www.mimecast.com/knowledgebase/KBID10473.htm#451
			// 451 Temporary local problem - please try later
			// 451 4.7.1 Service unavailable - try again later
			// 451 This server employs greylisting as a means of reducing spam. Your message has been delayed and will be accepted later.
			// 451 Greylisted
			// 452 4.3.1 Insufficient system storage
			// 454 4.7.1 Temp Spam Reject; Client host [173.203.70.224] deferred using hostkarma.junkemailfilter.com=127.0.0.2; Black listed at hostkarma http://ipadmin.junkemailfilter.com/remove.php?ip=173.203.70.224
			
		}


		

		[Test]
		public void SmtpResponseRegexPattern()
		{
			CheckSmtpResponseRegexPattern checker = 
				delegate(string msg, string expectedSmtpCode, string expectedNdrCode, string expectedDetail)
				{
					Match m = Regex.Match(msg, EventsManager.RegexPatterns.SmtpResponse, RegexOptions.IgnoreCase | RegexOptions.Multiline);

					if (!m.Success)
						return false;

					Assert.AreEqual(expectedSmtpCode, m.Groups["SmtpCode"].Value);
					Assert.AreEqual(expectedNdrCode, m.Groups["NdrCode"].Value);
					Assert.AreEqual(expectedDetail, m.Groups["Detail"].Value);

					return true;
				};

			Assert.IsTrue(checker(@"550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", "550", "5.1.1", "<bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com"));

			// Not sure how to handle these:
			//Assert.IsTrue(checker(@"550-5.1.1 The email account that you tried to reach does not exist. Please try
//550-5.1.1 double-checking the recipient's email address for typos or
//550-5.1.1 unnecessary spaces. Learn more at
//550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp", string.Empty, "5.1.1", "http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp"));

			Assert.IsTrue(checker(@"5.1.0 The email account that you tried to reach does not exist. Please try", string.Empty, "5.1.0", "The email account that you tried to reach does not exist. Please try"));
			Assert.IsTrue(checker(@"5.1.0 550 The email account that you tried to reach does not exist. Please try", "550", "5.1.0", "The email account that you tried to reach does not exist. Please try"));
			Assert.IsTrue(checker(@"552 No such user (users@domain.com)", "552", string.Empty, "No such user (users@domain.com)"));
			Assert.IsTrue(checker(@"5.1.5 more stuff", string.Empty, "5.1.5", "more stuff"));
		}




		/// <summary>
		/// Full checking of processing an SMTP response message.
		/// </summary>
		[Test]
		public void SmtpResponseBounceProcessing()
		{
			MantaBounceEvent mbEvent = null;


			// Check an AOL response.
			mbEvent = EventsManager.Instance.ProcessSmtpResponseMessage(@"550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", "bobobobobobobobobobobob@aol.com", 100);
			Assert.AreEqual(MantaBounceType.Hard, mbEvent.BounceInfo.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, mbEvent.BounceInfo.BounceCode);
			Assert.AreEqual("bobobobobobobobobobobob@aol.com", mbEvent.EmailAddress);
			Assert.AreEqual(MantaEventType.Bounce, mbEvent.EventType);
			Assert.AreEqual("550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", mbEvent.Message);
			// Need to get GetSendIdFromInternalSendId() method wired up before we can predict what this will give us.
			// Likely to be the current DateTime as a string if we provide an InternalSendID value that doesn't yet exist.
			// Assert.AreEqual("100", mbEvent.SendID);

			
			// Check a GMail response (multi-line).
			mbEvent = EventsManager.Instance.ProcessSmtpResponseMessage(@"550-5.1.1 The email account that you tried to reach does not exist. Please try
550-5.1.1 double-checking the recipient's email address for typos or
550-5.1.1 unnecessary spaces. Learn more at
550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp", "bobobobobobobobobobobob@gmail.com", 100);
			Assert.AreEqual(MantaBounceType.Hard, mbEvent.BounceInfo.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, mbEvent.BounceInfo.BounceCode);
			Assert.AreEqual("bobobobobobobobobobobob@gmail.com", mbEvent.EmailAddress);
			Assert.AreEqual(MantaEventType.Bounce, mbEvent.EventType);
			Assert.AreEqual(@"550-5.1.1 The email account that you tried to reach does not exist. Please try", mbEvent.Message);
		}
	}
}
