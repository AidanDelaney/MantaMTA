using Colony101.MTA.Library.OutboundRules;
using NUnit.Framework;

namespace MTALibraryTests
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
			OutboundRuleCollection rules = OutboundRuleManager.GetRules(new Colony101.MTA.Library.DNS.MXRecord("localhost", 10, 10), new Colony101.MTA.Library.MtaIpAddress.MtaIpAddress() { ID = 0 });
			Assert.AreEqual(3, rules.Count);
		}
	}
}
