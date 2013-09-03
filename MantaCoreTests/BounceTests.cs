using MantaMTA.Core.Enums;
using MantaMTA.Core.Events;
using MantaMTA.Core.Message;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class BounceTests : TestFixtureBase
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
		delegate bool CheckSmtpResponseRegexPattern(string msg, string expectedSmtpCode, string expectedNdrCode);


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
					new BouncePair{ BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable },
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
			EmailProcessingDetails processingDetails;
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
Diagnostic-Code: smtp;550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", out actualBouncePair, out bounceMessage, out processingDetails);
			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, actualBouncePair.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, actualBouncePair.BounceCode);
			Assert.AreEqual(@"550 5.1.1 <some.user@colony101.co.uk>: Recipient address rejected: colony101.co.uk", bounceMessage);

			// BenC (2013-08-23): The ProcessingResult is not set by .ParseBounceMessage() as it's not intended to be called directly so have hardcoded NotYetSet here.
			// Only reason it's being called directly is for unit testing.
			Assert.AreEqual(EmailProcessingResult.NotYetSet, processingDetails.ProcessingResult);

			Assert.AreEqual(BounceIdentifier.NdrCode, processingDetails.BounceIdentifier);
			Assert.AreEqual(0, processingDetails.MatchingBounceRuleID);
			Assert.AreEqual("5.1.1", processingDetails.MatchingValue);






			// Provide only a Status field to be parsed, no Diagnostic-Code.
			returned = EventsManager.Instance.ParseNdr(@"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Tue, 9 Oct 2012 19:02:10 +0100

Final-Recipient: rfc822;some.user@colony101.co.uk
Action: failed
Status: 5.1.1", out actualBouncePair, out bounceMessage, out processingDetails);
			Assert.AreEqual(true, returned);
			Assert.AreEqual(MantaBounceType.Hard, actualBouncePair.BounceType);
			Assert.AreEqual(MantaBounceCode.BadEmailAddress, actualBouncePair.BounceCode);
			Assert.AreEqual("5.1.1", bounceMessage);

			Assert.AreEqual(BounceIdentifier.NdrCode, processingDetails.BounceIdentifier);
			Assert.AreEqual(0, processingDetails.MatchingBounceRuleID);
			Assert.AreEqual("5.1.1", processingDetails.MatchingValue);

		}

		/// <summary>
		/// Check we're parsing bounce messages correctly.
		/// </summary>
		[Test]
		public void BounceMessageParsing()
		{
			#region Test Data
			// Be aware that any tests that use Bounce Rules may find that their RuleID values change.
			var testData = new []
			{
				// Commented out lots of these tests as they're looking at 4xx errors which aren't checked against the Bounce Rules, only 5xxs are.
				/*
				new 
				{
					Message = @"421 4.7.1 : (DYN:T1) http://postmaster.info.aol.com/errors/421dynt1.html", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.RateLimitedByReceivingMta },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.BounceRule, MatchingBounceRuleID = 3, MatchingValue = "DYN:T1" }
				},
				new 
				{
					Message = @"421 4.7.1 : (DYN:T2) some content", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.BounceRule, MatchingBounceRuleID = 12, MatchingValue =  "DYN:T2" }
				},
				new 
				{ 
					Message = @"421 4.7.1 Intrusion prevention active for [173.203.70.224][S]", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "4.7.1" }
				},
				new 
				{ 
					Message = @"450 4.1.1 <some.user@colony101.co.uk>: Recipient address rejected: unverified address: connect to mailgate.jtc65.co.uk[193.195.220.67]: Connection timed out", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress},
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "4.1.1" }
				},
				new 
				{ 
					Message = @"450 4.1.1 <some.user@colony101.co.uk>: Recipient address rejected: User unknown in virtual mailbox table", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessAbuse, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "4.1.1" }
				},
				new 
				{ 
					Message = @"450 4.2.0 <some.user@colony101.co.uk>: Recipient address rejected: Greylisted", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessAbuse, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "4.2.0" }
				},
				new 
				{ 
					Message = @"451 Requested action aborted: local error in processing (code: 11)", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.General },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.SmtpCode, MatchingBounceRuleID = 0, MatchingValue = "451" }
				},
				new 
				{ 
					Message = @"451 4.7.1 Access denied by DCC", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.General },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "4.7.1" }
				},
				new 
				{ 
					Message = @"421 4.7.0 [GL01] Message from (192.129.253.20) temporarily deferred - 4.16.50. Please refer to http://postmaster.yahoo.com/errors/postmaster-21.html", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.BounceRule, MatchingBounceRuleID = 0, MatchingValue = "421 4.7.0 [GL01]" }
				}
				*/
				new 
				{ 
					Message = @"551 You have sent an email to an address not recognised by our system. The email has been refused.", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.BadEmailAddress },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.SmtpCode, MatchingBounceRuleID = 0, MatchingValue = "551" }
				},
				new 
				{
					Message = @"550-5.7.1 recipient <EMAIL> unknown #292 (p6NBV6039032582700)
550-5.7.1 The line above says why the NorMAN mail gateways REJECTED this email.
550-5.7.1 Please see <http://www.ncl.ac.uk/iss/support/security/NORMAN_reject>
550 5.7.1 for a more detailed explanation.", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.General },
					ExpectedBounceProcessingDetails = new EmailProcessingDetails { ProcessingResult = EmailProcessingResult.SuccessBounce, BounceIdentifier = BounceIdentifier.NdrCode, MatchingBounceRuleID = 0, MatchingValue = "5.7.1" }
				}
			};
			#endregion



			for (int i = 0; i < testData.Length; i++)
			{
				var currentTestData = testData[i];
				EmailProcessingDetails processingDetails;
				BouncePair bouncePair;
				string bounceMessage = string.Empty;

				bool returned = false;
				using (CreateTransactionScopeObject())
				{
					returned = EventsManager.Instance.ParseBounceMessage(currentTestData.Message, out bouncePair, out bounceMessage, out processingDetails);
				}

				Assert.IsTrue(returned, currentTestData.Message);

				// Check the BouncePair values.
				Assert.AreEqual(currentTestData.ExpectedBouncePair.BounceCode, bouncePair.BounceCode, currentTestData.Message);
				Assert.AreEqual(currentTestData.ExpectedBouncePair.BounceType, bouncePair.BounceType, currentTestData.Message);



				// Check the EmailProcessingDetails values.
				Assert.AreEqual(currentTestData.ExpectedBounceProcessingDetails.BounceIdentifier, processingDetails.BounceIdentifier, currentTestData.Message);

				// BenC (2013-08-23): The ProcessingResult is not set by .ParseBounceMessage() as it's not intended to be called directly so have hardcoded NotYetSet here.
				// Only reason it's being called directly is for unit testing.
				Assert.AreEqual(EmailProcessingResult.NotYetSet, processingDetails.ProcessingResult, currentTestData.Message);

				Assert.AreEqual(currentTestData.ExpectedBounceProcessingDetails.MatchingBounceRuleID, processingDetails.MatchingBounceRuleID, currentTestData.Message);
				Assert.AreEqual(currentTestData.ExpectedBounceProcessingDetails.MatchingValue, processingDetails.MatchingValue, currentTestData.Message);
			}
		}


		
		/// <summary>
		/// Check the SmtpResponse Regex pattern works correctly.
		/// </summary>
		[Test]
		public void SmtpResponseRegexPattern()
		{
			CheckSmtpResponseRegexPattern checker = 
				delegate(string msg, string expectedSmtpCode, string expectedNdrCode)
				{
					Match m = Regex.Match(msg, EventsManager.RegexPatterns.SmtpResponse, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline);

					if (!m.Success)
						return false;

					Assert.AreEqual(expectedSmtpCode, m.Groups["SmtpCode"].Value);
					Assert.AreEqual(expectedNdrCode, m.Groups["NdrCode"].Value);
					Assert.AreEqual(msg, m.Groups["Detail"].Value);

					return true;
				};

			Assert.IsTrue(checker(@"550 5.1.1 <bobobobobobobobobobobob@aol.com>: Recipient address rejected: aol.com", "550", "5.1.1"));

			// Not sure how to handle these:
			Assert.IsTrue(checker(@"550-5.1.1 The email account that you tried to reach does not exist. Please try
			550-5.1.1 double-checking the recipient's email address for typos or
			550-5.1.1 unnecessary spaces. Learn more at
			550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 g8si5593977eet.3 - gsmtp", "550", "5.1.1"));

			Assert.IsTrue(checker(@"5.1.0 The email account that you tried to reach does not exist. Please try", string.Empty, "5.1.0"));
			Assert.IsTrue(checker(@"5.1.0 550 The email account that you tried to reach does not exist. Please try", string.Empty, "5.1.0"));
			Assert.IsTrue(checker(@"552 No such user (users@domain.com)", "552", string.Empty));
			Assert.IsTrue(checker(@"5.1.5 more stuff", string.Empty, "5.1.5"));
		}


		/// <summary>
		/// Check the SmtpResponse Regex pattern works correctly.
		/// </summary>
		[Test]
		public void NdrCodeRegexPattern()
		{
			Regex re = new Regex(EventsManager.RegexPatterns.NonDeliveryReportCode, RegexOptions.ExplicitCapture);

			// Check the pattern can pull out a typical NDR code, no matter where it appears.
			Assert.AreEqual("1.2.3", re.Match("1.2.3").Value);
			Assert.AreEqual("1.2.3", re.Match("Text before 1.2.3").Value);
			Assert.AreEqual("1.2.3", re.Match("1.2.3 text after").Value);
			Assert.AreEqual("1.2.3", re.Match("Text before 1.2.3 and text after").Value);

			// Check the pattern can pull out an NDR code that's the full size of what's specified in the RFC:
			// "x.y[1-3].z[1-3]" so "5.123.456".
			Assert.IsFalse(re.Match("111.222.333").Success);
			Assert.AreEqual("1.222.333", re.Match("1.222.333").Value);
			Assert.AreEqual("1.222.333", re.Match("Text before 1.222.333").Value);
			Assert.AreEqual("1.222.333", re.Match("1.222.333 text after").Value);
			Assert.AreEqual("1.222.333", re.Match("Text before 1.222.333 and text after").Value);

			// Check it gets the first match only.
			Assert.AreEqual("1.222.333", re.Match("Text before 1.222.333 and text after 4.555.666 and more text after").Value);
		}

		/// <summary>
		/// Check we can correctly process a NonDelivery Report.
		/// </summary>
		[Test]
		public void NonDeliveryReport()
		{
			using (CreateTransactionScopeObject())
			{
				string emailContent = System.IO.File.OpenText(@".\..\..\NonDeliveryReport Test Email.eml").ReadToEnd();

				EmailProcessingDetails processingDetail = EventsManager.Instance.ProcessBounceEmail(emailContent);

				Assert.AreEqual(EmailProcessingResult.SuccessBounce, processingDetail.ProcessingResult);
				MantaEventCollection events = EventsManager.Instance.GetEvents();
				Assert.AreEqual(1, events.Count);
				Assert.IsTrue(events[0] is MantaBounceEvent);
				MantaBounceEvent bounce = (MantaBounceEvent)events[0];
				Assert.AreEqual(MantaBounceCode.BadEmailAddress, bounce.BounceInfo.BounceCode);
				Assert.AreEqual(MantaBounceType.Hard, bounce.BounceInfo.BounceType);
				Assert.AreEqual("some.user@colony101.co.uk", bounce.EmailAddress);
				Assert.AreEqual("550 5.1.1 unknown or illegal alias: some.user@colony101.co.uk", bounce.Message);
				Assert.AreEqual("TestData", bounce.SendID);
			}
		}


		/// <summary>
		/// Test to check AOL NDR works as expected.
		/// </summary>
		[Test]
		public void NDR_AOL()
		{
			string ndr = @"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Mon, 15 Jul 2013 19:01:58 +0100

Final-Recipient: rfc822;artymall@aol.com
Action: failed
Status: 5.1.1
Diagnostic-Code: smtp;550 5.1.1 <artymall@aol.com>: Recipient address rejected: aol.com";
			TestNdr(ndr, MantaBounceCode.BadEmailAddress, MantaBounceType.Hard, "550 5.1.1 <artymall@aol.com>: Recipient address rejected: aol.com");
		}

		/// <summary>
		/// Test to check GMail NDR works as expected.
		/// </summary>
		[Test]
		public void NDR_GMail()
		{
			string ndr = @"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Mon, 15 Jul 2013 18:35:44 +0100

Final-Recipient: rfc822;some.user@gmail.com
Action: failed
Status: 5.5.0
Diagnostic-Code: smtp;550-5.1.1 The email account that you tried to reach does not exist. Please try
550-5.1.1 double-checking the recipient's email address for typos or
550-5.1.1 unnecessary spaces. Learn more at
550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 eq9si1178242wib.32 - gsmtp";
			TestNdr(ndr, MantaBounceCode.BadEmailAddress, MantaBounceType.Hard, @"550-5.1.1 The email account that you tried to reach does not exist. Please try
550-5.1.1 double-checking the recipient's email address for typos or
550-5.1.1 unnecessary spaces. Learn more at
550 5.1.1 http://support.google.com/mail/bin/answer.py?answer=6596 eq9si1178242wib.32 - gsmtp");
		}

		/// <summary>
		/// Test to check Hotmail NDR works as expected.
		/// </summary>
		[Test]
		public void NDR_Hotmail()
		{
			string ndr = @"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Mon, 15 Jul 2013 19:14:12 +0100

Final-Recipient: rfc822;some.user@hotmail.com
Action: failed
Status: 5.5.0
Diagnostic-Code: smtp;550 Requested action not taken: mailbox unavailable";
			TestNdr(ndr, MantaBounceCode.BadEmailAddress, MantaBounceType.Hard, "550 Requested action not taken: mailbox unavailable");
		}

		/// <summary>
		/// Test to check Yahoo NDR works as expected.
		/// </summary>
		[Test]
		public void NDR_Yahoo()
		{
			string ndr = @"Reporting-MTA: dns;snt3.net
Received-From-MTA: dns;SNT3
Arrival-Date: Mon, 15 Jul 2013 18:29:22 +0100

Final-Recipient: rfc822;some.user@yahoo.com
Action: failed
Status: 5.5.0
Diagnostic-Code: smtp;554 delivery error: dd This user doesn't have a yahoo.com account (some.user@yahoo.com) [0] - mta1184.mail.ir2.yahoo.com";
			TestNdr(ndr, MantaBounceCode.BadEmailAddress, MantaBounceType.Hard, "554 delivery error: dd This user doesn't have a yahoo.com account (some.user@yahoo.com) [0] - mta1184.mail.ir2.yahoo.com");
		}

		/// <summary>
		/// Run an NDR Test.
		/// </summary>
		/// <param name="ndr">The NDR body part content.</param>
		/// <param name="expectedCode">The expected Manta Bounce Code.</param>
		/// <param name="expectedType">The expected Manta Bounce Type.</param>
		/// <param name="expectedMessage">The expected report message.</param>
		private void TestNdr(string ndr, MantaBounceCode expectedCode, MantaBounceType expectedType, string expectedMessage)
		{
			using (CreateTransactionScopeObject())
			{
				string bounceMessage = string.Empty;
				BouncePair bouncePair;
				EmailProcessingDetails processingDetails;

				bool returned = EventsManager.Instance.ParseNdr(ndr, out bouncePair, out bounceMessage, out processingDetails);
				Assert.IsTrue(returned);
				Assert.AreEqual(expectedCode, bouncePair.BounceCode);
				Assert.AreEqual(expectedType, bouncePair.BounceType);
				Assert.AreEqual(expectedMessage, bounceMessage);
			}
		}


		/// <summary>
		/// Dig into an email that contains a deep body part for a message/delivery-status report.
		/// </summary>
		[Test]
		public void FindDeepDeliveryReport()
		{
			string emailContent = System.IO.File.OpenText(@".\..\..\Many BodyParts - structure.eml").ReadToEnd();
			MimeMessage msg = MimeMessage.Parse(emailContent);
			
			BodyPart deliveryReportBodyPart;

			Assert.IsTrue(EventsManager.Instance.FindFirstBodyPartByMediaType(msg.BodyParts, "message/delivery-status", out deliveryReportBodyPart));
			Assert.AreEqual(@"Reporting-MTA: dns;someserver.com
Received-From-MTA: dns;mail.someserver.com
Arrival-Date: Fri, 12 Jul 2013 10:09:28 +0000

Original-Recipient: rfc822;someone@someserver.com
Final-Recipient: rfc822;finalsomeone@someserver.com
Action: failed
Status: 5.2.2
Diagnostic-Code: smtp;554-5.2.2 mailbox full

", deliveryReportBodyPart.GetDecodedBody());
		}
	}
}