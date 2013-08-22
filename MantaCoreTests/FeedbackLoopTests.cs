using MantaMTA.Core.Events;
using MantaMTA.Core.Message;
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
	}
}
