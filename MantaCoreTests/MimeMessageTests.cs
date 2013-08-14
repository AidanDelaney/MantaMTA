using MantaMTA.Core.Message;
using NUnit.Framework;
using System.IO;
using System.Net.Mime;
using MantaMTA.Core;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	class MimeMessageTests
	{
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

			ContentType msgContentType = new ContentType(msg.Headers.GetFirstOrDefault("Content-Type").Value);
			Assert.AreEqual("unique-boundary-1", msgContentType.Boundary);

			Assert.AreEqual(5, msg.BodyParts.Length);


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
			Assert.AreEqual(2, bodyPart.BodyParts.Length);


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
	}
}
