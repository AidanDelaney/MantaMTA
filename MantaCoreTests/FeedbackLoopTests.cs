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
			MimeMessage msg = MimeMessage.Parse(FeedbackLoopEmails.AolAbuse);
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(msg);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}

		[Test]
		public void Hotmail()
		{
			MimeMessage msg = MimeMessage.Parse(FeedbackLoopEmails.HotmailAbuse);
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(msg);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}

		[Test]
		public void Yahoo()
		{
			MimeMessage msg = MimeMessage.Parse(FeedbackLoopEmails.YahooAbuse);
			EmailProcessingResult result = EventsManager.Instance.ProcessFeedbackLoop(msg);
			Assert.AreEqual(EmailProcessingResult.SuccessAbuse, result);
		}
	}
}
