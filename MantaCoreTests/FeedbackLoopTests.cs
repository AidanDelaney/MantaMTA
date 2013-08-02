using MantaMTA.Core.Events;
using MantaMTA.Core.Message;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class FeedbackLoopTests
	{
		[Test]
		public void Aol()
		{
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.AolAbuse);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}

		[Test]
		public void Hotmail()
		{
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.HotmailAbuse);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}

		[Test]
		public void Yahoo()
		{
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(FeedbackLoopEmails.YahooAbuse);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}
	}
}
