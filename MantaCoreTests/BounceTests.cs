using MantaMTA.Core;
using MantaMTA.Core.Events;
using MantaMTA.Core.Message;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using MantaMTA.Core.Events;
using NUnit.Framework;

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
			Assert.AreEqual("5.1.1", bounceMessage);
		}

		/// <summary>
		/// Check we're parsing bounce messages correctly.
		/// </summary>
		[Test]
		public void BounceMessageParsing()
		{
			#region Test Data
			var testData = new []
			{ 
				new 
				{ 
					Message = @"421 4.7.1 : (DYN:T1) http://postmaster.info.aol.com/errors/421dynt1.html", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.RateLimitedByReceivingMta }
				},
				new 
				{ 
					Message = @"421 4.7.1 : (DYN:T2) some content", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable }
				},
				new 
				{ 
					Message = @"421 4.7.1 Intrusion prevention active for [173.203.70.224][S]", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable }
				},
				new 
				{ 
					Message = @"450 4.1.1 <some.user@colony101.co.uk>: Recipient address rejected: unverified address: connect to mailgate.jtc65.co.uk[193.195.220.67]: Connection timed out", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress}
				},
				new 
				{ 
					Message = @"450 4.1.1 <some.user@colony101.co.uk>: Recipient address rejected: User unknown in virtual mailbox table", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress }
				},
				new 
				{ 
					Message = @"450 4.2.0 <some.user@colony101.co.uk>: Recipient address rejected: Greylisted", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.BadEmailAddress }
				},
				new 
				{ 
					Message = @"451 Requested action aborted: local error in processing (code: 11)", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.General }
				},
				new 
				{ 
					Message = @"451 4.7.1 Access denied by DCC", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.General }
				},
				new 
				{ 
					Message = @"551 You have sent an email to an address not recognised by our system. The email has been refused.", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.BadEmailAddress }
				},
				new 
				{
					Message = @"550-5.7.1 recipient <EMAIL> unknown #292 (p6NBV6039032582700)
550-5.7.1 The line above says why the NorMAN mail gateways REJECTED this email.
550-5.7.1 Please see <http://www.ncl.ac.uk/iss/support/security/NORMAN_reject>
550 5.7.1 for a more detailed explanation.", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Hard, BounceCode = MantaBounceCode.BadEmailAddress }
				},
				new 
				{ 
					Message = @"421 4.7.0 [GL01] Message from (192.129.253.20) temporarily deferred - 4.16.50. Please refer to http://postmaster.yahoo.com/errors/postmaster-21.html", 
					ExpectedBouncePair = new BouncePair { BounceType = MantaBounceType.Soft, BounceCode = MantaBounceCode.ServiceUnavailable }
				}
			};
			#endregion

			for (int i = 0; i < testData.Length; i++)
			{
				var currentTestData = testData[i];
				BouncePair bouncePair;
				string bounceMessage = string.Empty;

				bool returned = false;
				using (CreateTransactionScopeObject())
				{
					returned = EventsManager.Instance.ParseBounceMessage(currentTestData.Message, out bouncePair, out bounceMessage);
				}
				Assert.IsTrue(returned, currentTestData.Message);
				Assert.AreEqual(currentTestData.ExpectedBouncePair.BounceCode, bouncePair.BounceCode, currentTestData.Message);
				Assert.AreEqual(currentTestData.ExpectedBouncePair.BounceType, bouncePair.BounceType, currentTestData.Message);
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

				bool returned = EventsManager.Instance.ParseNdr(ndr, out bouncePair, out bounceMessage);
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
			
			string deliveryReport = string.Empty;

			Assert.IsTrue(EventsManager.Instance.FindDeliveryReport(msg.BodyParts, out deliveryReport));
			Assert.AreEqual(@"Reporting-MTA: dns;someserver.com
Received-From-MTA: dns;mail.someserver.com
Arrival-Date: Fri, 12 Jul 2013 10:09:28 +0000

Original-Recipient: rfc822;someone@someserver.com
Final-Recipient: rfc822;finalsomeone@someserver.com
Action: failed
Status: 5.2.2
Diagnostic-Code: smtp;554-5.2.2 mailbox full

", deliveryReport);
		}


		/// <summary>
		/// Check we can unfold email headers and separate headers and body.
		/// </summary>
		[Test]
		public void SplitHeadersAndBody()
		{
			string headers, body;

			// No headers, just a body.
			MimeMessage.SeparateBodyPartHeadersAndBody(MtaParameters.NewLine + "Just a body.", out headers, out body);
			Assert.AreEqual(string.Empty, headers);
			Assert.AreEqual("Just a body.", body);

			// Couple of headers and a body.
			MimeMessage.SeparateBodyPartHeadersAndBody("From: <someone@somewhere.com>" + MtaParameters.NewLine + "To: <receiver@receiving.co.uk>" + MtaParameters.NewLine + MtaParameters.NewLine + "Here's the body.", out headers, out body);
			Assert.AreEqual("From: <someone@somewhere.com>" + MtaParameters.NewLine + "To: <receiver@receiving.co.uk>", headers);
			Assert.AreEqual("Here's the body.", body);


			// Check headers are separated correctly.
			MessageHeaderCollection headersColl = MimeMessage.GetMessageHeaders("From: <someone@somewhere.com>" + MtaParameters.NewLine + "To: <receiver@receiving.co.uk>");
			Assert.AreEqual("From", headersColl[0].Name);
			Assert.AreEqual("<someone@somewhere.com>", headersColl[0].Value);
			Assert.AreEqual("To", headersColl[1].Name);
			Assert.AreEqual("<receiver@receiving.co.uk>", headersColl[1].Value);


			// Check headers are unfolded correctly, ensuring that only "\r\n" is used as a line ending ("\r" and "\n" may appear, but should be considered
			// as part of a header's value.
			headersColl = MimeMessage.GetMessageHeaders("FoldedHeader1: beginning" + MtaParameters.NewLine + " end" + MtaParameters.NewLine + "FoldedHeader2: beginning" + MtaParameters.NewLine + "\t\tend" + MtaParameters.NewLine + "From: <someone@somewhere.com>" + MtaParameters.NewLine + "To: <receiver@receiving.co.uk>" + MtaParameters.NewLine + "FoldedHeader3: line1\nstill line 1" + MtaParameters.NewLine + "\t\tline2\rstill line 2");
			Assert.AreEqual("FoldedHeader1", headersColl[0].Name);
			Assert.AreEqual("beginning end", headersColl[0].Value);
			Assert.AreEqual("FoldedHeader2", headersColl[1].Name);
			Assert.AreEqual("beginning\t\tend", headersColl[1].Value);
			Assert.AreEqual("From", headersColl[2].Name);
			Assert.AreEqual("<someone@somewhere.com>", headersColl[2].Value);
			Assert.AreEqual("To", headersColl[3].Name);
			Assert.AreEqual("<receiver@receiving.co.uk>", headersColl[3].Value);
			Assert.AreEqual("FoldedHeader3", headersColl[4].Name);
			Assert.AreEqual("line1\nstill line 1\t\tline2\rstill line 2", headersColl[4].Value);
		}


		/// <summary>
		/// Check that we can parse a MIME message that only has a single bodypart so no boundaries.
		/// </summary>
		[Test]
		public void ParseMimeMessageWithSingleBodyPart()
		{
			string emailContent = string.Format("MIME-Version: 1.0{0}Content-Type: text/plain;{0}\tcharset=\"ISO-8859-1\"{0}Content-Transfer-Encoding: quoted-printableFrom: <someone@somewhere.com>{0}To: <recipient@receiver.co.uk>{0}Subject: my subject{0}{0}Hello, this is the email.{0}{0}Here's a second line of content.{0}", MtaParameters.NewLine);

			MimeMessage msg = MimeMessage.Parse(emailContent);
		}


		/// <summary>
		/// Checks a MIME encoded string can be correctly converted into a MimeMessage object.
		/// </summary>
		[Test]
		public void ParseMimeMessage()
		{
			BodyPart bodyPart = null;

			string emailContent = System.IO.File.OpenText(@".\..\..\A Complex Multipart Example.eml").ReadToEnd();

			MimeMessage msg = MimeMessage.Parse(emailContent);

			ContentType msgContentType = new ContentType(msg.Headers.GetFirst("Content-Type").Value);
			Assert.AreEqual("unique-boundary-1", msgContentType.Boundary);

			Assert.AreEqual(5, msg.BodyParts.Count());


			#region Check the first level of body parts.

			bodyPart = msg.BodyParts[0];
			Assert.AreEqual("text/plain", bodyPart.ContentType.MediaType);
			Assert.AreEqual("us-ascii", bodyPart.ContentType.CharSet.ToLower());
			Assert.AreEqual(TransferEncoding.SevenBit, bodyPart.TransferEncoding);
			Assert.AreEqual(@"    ...Some text appears here...
[Note that the preceding blank line means
no header fields were given and this is text,
with charset US ASCII.  It could have been
done with explicit typing as in the next part.]
", bodyPart.GetDecodedBody());

			bodyPart = msg.BodyParts[1];
			Assert.AreEqual("text/plain", bodyPart.ContentType.MediaType);
			Assert.AreEqual("us-ascii", bodyPart.ContentType.CharSet.ToLower());
			Assert.AreEqual(TransferEncoding.SevenBit, bodyPart.TransferEncoding);
			Assert.AreEqual(@"This could have been part of the previous part,
but illustrates explicit versus implicit
typing of body parts.
", bodyPart.GetDecodedBody());

			bodyPart = msg.BodyParts[2];
			Assert.AreEqual("multipart/mixed", bodyPart.ContentType.MediaType);


			bodyPart = msg.BodyParts[3];
			Assert.AreEqual("text/richtext", bodyPart.ContentType.MediaType);
			Assert.AreEqual("us-ascii", bodyPart.ContentType.CharSet.ToLower());
			Assert.AreEqual(TransferEncoding.SevenBit, bodyPart.TransferEncoding);
			Assert.AreEqual(@"This is <bold><italic>richtext.</italic></bold>
<smaller>as defined in RFC 1341</smaller>
<nl><nl>Isn't it
<bigger><bigger>cool?</bigger></bigger>
", bodyPart.GetDecodedBody());

			bodyPart = msg.BodyParts[4];
			Assert.AreEqual("message/rfc822", bodyPart.ContentType.MediaType);
			Assert.AreEqual(@"From: (mailbox in US-ASCII)
To: (address in US-ASCII)
Subject: (subject in US-ASCII)
Content-Type: Text/plain; charset=ISO-8859-1
Content-Transfer-Encoding: Quoted-printable

    ... Additional text in ISO-8859-1 goes here ...
", bodyPart.GetDecodedBody());

			#endregion Check the first level of body parts.



			// Check the multipart bodyPart.
			bodyPart = msg.BodyParts[2];
			Assert.AreEqual("unique-boundary-2", bodyPart.ContentType.Boundary);
			Assert.AreEqual(2, bodyPart.BodyParts.Count());


			Assert.AreEqual("audio/basic", bodyPart.BodyParts[0].ContentType.MediaType);
			Assert.AreEqual(TransferEncoding.Base64, bodyPart.BodyParts[0].TransferEncoding);
			Assert.AreEqual(@"    ... base64-encoded 8000 Hz single-channel
        mu-law-format audio data goes here....
", bodyPart.BodyParts[0].EncodedBody);

			Assert.AreEqual("image/gif", bodyPart.BodyParts[1].ContentType.MediaType);
			Assert.AreEqual(TransferEncoding.Base64, bodyPart.BodyParts[1].TransferEncoding);
			Assert.AreEqual(@"    ... base64-encoded image data goes here....
", bodyPart.BodyParts[1].EncodedBody);
		}


		/// <summary>
		/// Checking the StringReader extension method .ReadToCrLf() works.
		/// Gives us the equivalent of .ReadLine() that only considers "\r\n" (CRLF) to be the
		/// end of a line.  This is useful as Mime messages may contain "\r" or "\n" which isn't
		/// intended to indicate the end of a line.
		/// </summary>
		[Test]
		public void StringReaderReadToCrLfExtensionMethodCheck()
		{
			StringReader msr = new StringReader("line 1\r\nline 2\rstill line 2\r\nline 3\nstill line 3");

			Assert.AreEqual("line 1", msr.ReadToCrLf());
			Assert.AreEqual("line 2\rstill line 2", msr.ReadToCrLf());
			Assert.AreEqual("line 3\nstill line 3", msr.ReadToCrLf());
		}
	}
}