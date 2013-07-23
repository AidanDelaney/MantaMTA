using MantaMTA.Core.Enums;
using MantaMTA.Core.Sends;
using MantaMTA.Core.ServiceContracts;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class QueueControl : TestFixtureBase
	{
		[Test]
		public void PauseAndResume()
		{
			using (CreateTransactionScopeObject())
			{
				SendManager.Instance.StartService();
				
				Send snd = MantaMTA.Core.DAL.SendDB.CreateAndGetInternalSendID("testing");
				Assert.AreEqual(SendStatus.Active, snd.SendStatus);

				SendManager.Instance.Pause(snd.InternalID);
				snd = SendManager.Instance.GetSend(snd.ID);

				Assert.AreEqual(SendStatus.Paused, snd.SendStatus);

				SendManager.Instance.Resume(snd.InternalID);
				snd = SendManager.Instance.GetSend(snd.ID);

				Assert.AreEqual(SendStatus.Active, snd.SendStatus);
			}
		}

		[Test]
		public void DiscardAndResume()
		{
			using (CreateTransactionScopeObject())
			{
				Send snd = MantaMTA.Core.DAL.SendDB.CreateAndGetInternalSendID("testing");
				Assert.AreEqual(SendStatus.Active, snd.SendStatus);

				SendManager.Instance.Discard(snd.InternalID);
				snd = SendManager.Instance.GetSend(snd.ID);

				Assert.AreEqual(SendStatus.Discard, snd.SendStatus);

				SendManager.Instance.Resume(snd.InternalID);
				snd = SendManager.Instance.GetSend(snd.ID);

				Assert.AreEqual(SendStatus.Active, snd.SendStatus);
			}
		}
	}
}
