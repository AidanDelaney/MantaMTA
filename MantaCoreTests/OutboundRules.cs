using MantaMTA.Core.OutboundRules;
using NUnit.Framework;

namespace MantaMTA.Core.Tests
{
	[TestFixture]
	public class OutboundRules
	{
		/// <summary>
		/// Test tomake sure that we can get the default values from the database.
		/// </summary>
		[Test]
		public void TestDefaultRules()
		{
			int mxPatternID = 0;
			OutboundRuleCollection rules = OutboundRuleManager.GetRules(new MantaMTA.Core.DNS.MXRecord("localhost", 10, 10), new MantaMTA.Core.MtaIpAddress.MtaIpAddress() { ID = 0, IPAddress = System.Net.IPAddress.Parse("127.0.0.1") }, out mxPatternID);
			Assert.AreEqual(3, rules.Count);
		}
	}
}
