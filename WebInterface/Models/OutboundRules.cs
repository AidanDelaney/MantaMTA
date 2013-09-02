using MantaMTA.Core.OutboundRules;

namespace WebInterface.Models
{
	/// <summary>
	/// Holds the Model for the Outbound Rules page.
	/// </summary>
	public class OutboundRuleModel
	{
		/// <summary>
		/// Collection of the Outbound Rules
		/// </summary>
		public OutboundRuleCollection OutboundRules { get; set; }
		
		/// <summary>
		/// The MX pattern that the rules relate to.
		/// </summary>
		public OutboundMxPattern Pattern { get; set; }

		public OutboundRuleModel(OutboundRuleCollection outboundRuleCollection, OutboundMxPattern pattern)
		{
			OutboundRules = outboundRuleCollection;
			Pattern = pattern;
		}
	}
}