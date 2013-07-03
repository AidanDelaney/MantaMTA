using MantaMTA.Core.Message;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	class MimeMessageTests
	{
		/// <summary>
		/// Test what happens when a mime message is passed something it can't handle.
		/// Should return null.
		/// </summary>
		[Test]
		public void NotAMimeMessage()
		{
			string testMessage = "Not a mime message";
			MimeMessage msg = MimeMessage.Parse(testMessage);
			Assert.IsNull(msg);
		}

		/// <summary>
		/// Test attempting to parse a mime message with no content-type header.
		/// Should return null.
		/// </summary>
		[Test]
		public void NoContentTypeHeader()
		{
			string testMessage = @"from: <test@remote>
to: <test@localhost>

Hi";
			MimeMessage msg = MimeMessage.Parse(testMessage);
			Assert.IsNull(msg);
		}

		/// <summary>
		/// Test a non multipart message.
		/// Should be null.
		/// </summary>
		[Test]
		public void NotMultipart()
		{
			string testMessage = @"from: <test@remote>
to: <test@localhost>
content-type: text/plain

Hi";
			MimeMessage msg = MimeMessage.Parse(testMessage);
			Assert.IsNull(msg);
		}

		/// <summary>
		/// Test a mime message with no child messages in any body parts.
		/// </summary>
		[Test]
		public void NoChildMessages()
		{
			string testMessage = @"from: <test@remote>
to: <test@localhost>
content-type: multipart/mime

--boundry_one

Body Part One!

--boundry_one

Body Part Two!

--boundry_one--";
			MimeMessage msg = MimeMessage.Parse(testMessage);
			Assert.NotNull(msg);
			Assert.AreEqual(2, msg.BodyParts.Length);
		}

		/// <summary>
		/// Test a mime message with a body part that contains a message.
		/// </summary>
		[Test]
		public void ChildMessage()
		{
			string testMessage = @"from: <test@remote>
to: <test@localhost>
content-type: multipart/mime

--boundry_one

Body Part One!

--boundry_one Content-Type: message/rfc822

To: <test@remote>
From: <test@local>
Content-Type: multipart/mime

--boundry_two 

Body Part Two.One!

--boundry_two

Body Part Two.Two!

--boundy_two--

--boundry_one--";
			MimeMessage msg = MimeMessage.Parse(testMessage);
			Assert.NotNull(msg);
			Assert.AreEqual(2, msg.BodyParts.Length);

			Assert.IsFalse(msg.BodyParts[0].HasChildMimeMessage);
			Assert.IsTrue(msg.BodyParts[1].HasChildMimeMessage);
			
			Assert.NotNull(msg.BodyParts[1].ChildMimeMessage);
			Assert.NotNull(msg.BodyParts[1].ChildMimeMessage.BodyParts);
		}
	}
}
