using System;
using WebInterfaceLib.BO;

namespace WebInterface.Models
{
	/// <summary>
	/// Model for the bounce page.
	/// </summary>
	public class BounceModel
	{
		/// <summary>
		/// Holds the bounce info.
		/// </summary>
		public BounceInfo[] BounceInfo { get; set; }

		/// <summary>
		/// Holds the current page number.
		/// </summary>
		public int CurrentPage { get; set; }

		/// <summary>
		/// Holds the total pages required to view all bounce info.
		/// </summary>
		public int PageCount { get; set; }
		
		/// <summary>
		/// Holds the deferred attempts count.
		/// </summary>
		public long DeferredCount { get; set; }

		/// <summary>
		/// Holds the rejected attepts count.
		/// </summary>
		public long RejectedCount { get; set; }
	}
}