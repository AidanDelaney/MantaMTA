using System.IO;
using Colony101.MTA.Library;
using Colony101.MTA.Library.Enums;
using NUnit.Framework;

namespace MTALibraryTests
{
	[TestFixture]
	public class SmtpStreamHandlerTests : TestFixtureBase
	{
		/// <summary>
		/// Test to make sure that SmtpStreamHandlers encoding is working correctly.
		/// </summary>
		[Test]
		public void TestUnicode()
		{
			string unicodeStr = "को कथा";
			using(MemoryStream ms = new MemoryStream())
			{
				SmtpStreamHandler stream = new SmtpStreamHandler(ms);
				stream.SetSmtpTransportMIME(SmtpTransportMIME._8BitUTF);
				stream.WriteLine(unicodeStr, false);
				ms.Position = 0;
				string result = stream.ReadLine(false);
				Assert.AreEqual(unicodeStr, result);
			}

			using (MemoryStream ms = new MemoryStream())
			{
				SmtpStreamHandler stream = new SmtpStreamHandler(ms);
				stream.SetSmtpTransportMIME(SmtpTransportMIME._7BitASCII);
				stream.WriteLine(unicodeStr, false);
				ms.Position = 0;
				string result = stream.ReadLine(false);
				Assert.AreNotEqual(unicodeStr, result);
			}
		}
	}
}
