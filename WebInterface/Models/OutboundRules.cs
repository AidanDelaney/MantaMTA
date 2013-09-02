using MantaMTA.Core.OutboundRules;

namespace WebInterface.Models
{
	public class OutboundRuleModel
	{
		public OutboundRuleCollection OutboundRules { get; set; }
		public OutboundMxPattern Pattern { get; set; }

		public OutboundRuleModel(OutboundRuleCollection outboundRuleCollection, OutboundMxPattern pattern)
		{
			OutboundRules = outboundRuleCollection;
			Pattern = pattern;
		}
	}
}