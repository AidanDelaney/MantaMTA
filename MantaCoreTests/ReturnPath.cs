using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MantaMTA.Core.Message;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class ReturnPathTest
	{
		[Test]
		public void Generate()
		{
			string returnPath = ReturnPathManager.GenerateReturnPath("test@remote", 10);
			Assert.AreEqual("return-test=remote-A@localhost", returnPath);
		}

		[Test]
		public void Decode()
		{
			string rcptTo = string.Empty;
			int internalSendID = -1;
			bool decoded = ReturnPathManager.TryDecode("return-test=remote-A@localhost", out rcptTo, out internalSendID);
			Assert.True(decoded);
			Assert.AreEqual("test@remote", rcptTo);
			Assert.AreEqual(10, internalSendID);
		}
	}
}
