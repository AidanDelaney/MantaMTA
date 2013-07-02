using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MantaMTA.Core.Events.Enums
{
	/// <summary>
	/// Used to identify how to use a Bounce Rule's Criteria property.
	/// </summary>
	internal enum BounceRuleType
	{
		/// <summary>
		/// Default value.
		/// </summary>
		Unknown = 0,
		/// <summary>
		/// The criteria is a Regex pattern to run against the message.
		/// </summary>
		RegularExpressionPattern = 1,
		/// <summary>
		/// The criteria is a string that may appear within the message.
		/// </summary>
		StringMatch = 2
	}
}
