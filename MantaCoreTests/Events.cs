using System;
using MantaMTA.Core.Events;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class Events : TestFixtureBase
	{
		/// <summary>
		/// Test ensures we can save a MantaBounceEvent to the database and get it back.
		/// </summary>
		[Test]
		public void SaveAndGetBounce()
		{
			using (CreateTransactionScopeObject())
			{
				MantaBounceEvent originalEvt = new MantaBounceEvent
				{
					BounceInfo = new BouncePair
					{
						BounceCode = MantaBounceCode.BadEmailAddress,
						BounceType = MantaBounceType.Hard
					},
					EmailAddress = "some.user@colony101.co.uk",
					EventTime = DateTime.Now,
					EventType = MantaEventType.Bounce,
					Message = "550 Invalid Inbox",
					SendID = "qwerty"
				};

				originalEvt.ID = EventsManager.Instance.Save(originalEvt);

				MantaBounceEvent savedEvt = (MantaBounceEvent)EventsManager.Instance.GetEvent(originalEvt.ID);

				Assert.NotNull(savedEvt);
				Assert.AreEqual(originalEvt.BounceInfo.BounceCode, savedEvt.BounceInfo.BounceCode);
				Assert.AreEqual(originalEvt.BounceInfo.BounceType, savedEvt.BounceInfo.BounceType);
				Assert.AreEqual(originalEvt.EmailAddress, savedEvt.EmailAddress);
				Assert.That(savedEvt.EventTime, Is.EqualTo(originalEvt.EventTime).Within(TimeSpan.FromSeconds(1)));
				Assert.AreEqual(originalEvt.EventType, savedEvt.EventType);
				Assert.AreEqual(originalEvt.ID, savedEvt.ID);
				Assert.AreEqual(originalEvt.Message, savedEvt.Message);
				Assert.AreEqual(originalEvt.SendID, savedEvt.SendID);
			}
		}

		/// <summary>
		/// Test ensures we can save a MantaAbuseEvent to the database and get it back.
		/// </summary>
		[Test]
		public void SaveAndGetAbuse()
		{
			using (CreateTransactionScopeObject())
			{
				MantaAubseEvent origAbuse = new MantaAubseEvent
				{
					EmailAddress = "some.user@colony101.co.uk",
					EventTime = DateTime.Now,
					EventType = MantaEventType.Abuse,
					SendID = "qwerty"
				};

				origAbuse.ID = EventsManager.Instance.Save(origAbuse);
				MantaAubseEvent savedAbuse = (MantaAubseEvent)EventsManager.Instance.GetEvent(origAbuse.ID);
				Assert.NotNull(savedAbuse);
				Assert.AreEqual(origAbuse.EmailAddress, savedAbuse.EmailAddress);
				Assert.That(savedAbuse.EventTime, Is.EqualTo(origAbuse.EventTime).Within(TimeSpan.FromSeconds(1)));
				Assert.AreEqual(origAbuse.EventType, savedAbuse.EventType);
				Assert.AreEqual(origAbuse.ID, savedAbuse.ID);
				Assert.AreEqual(origAbuse.SendID, savedAbuse.SendID);
			}
		}
	}
}
