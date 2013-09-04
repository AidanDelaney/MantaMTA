using MantaMTA.Core.MtaIpAddress;
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

		/// <summary>
		/// Holds a list of all the outbound Virtual MTAs.
		/// </summary>
		public MtaIpAddressCollection VirtualMtaCollection { get; set; }

		public OutboundRuleModel(OutboundRuleCollection outboundRuleCollection, OutboundMxPattern pattern, MtaIpAddressCollection virtualMtaCollection)
		{
			OutboundRules = outboundRuleCollection;
			Pattern = pattern;
			VirtualMtaCollection = virtualMtaCollection;
		}
	}
}