﻿using MantaMTA.Core.Enums;
using MantaMTA.Core.Events;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class FeedbackLoopTests : TestFixtureBase
	{
		[Test]
		public void Aol()
		{
			using (CreateTransactionScopeObject())
			{
				EmailProcessingDetails processingDetails = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.AolAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, processingDetails.ProcessingResult);
			}
		}

		[Test]
		public void Hotmail()
		{
			using (CreateTransactionScopeObject())
			{
				EmailProcessingDetails processingDetails = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.HotmailAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, processingDetails.ProcessingResult);
			}
		}

		[Test]
		public void Yahoo()
		{
			using (CreateTransactionScopeObject())
			{
				EmailProcessingDetails processingDetails = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.YahooAbuse);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, processingDetails.ProcessingResult);
			}
		}

		[Test]
		public void Fastmail()
		{
			using (CreateTransactionScopeObject())
			{
				EmailProcessingDetails processingDetails = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.FastMail);
				Assert.AreEqual(EmailProcessingResult.SuccessAbuse, processingDetails.ProcessingResult);
			}
		}
	}
}
