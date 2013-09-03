using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MantaMTA.Core.Enums;

namespace WebInterfaceLib.BO
{
	/// <summary>
	/// Holds information about a bounce.
	/// </summary>
	public class BounceInfo
	{
		/// <summary>
		/// The TransactionStatus that the bounce relates to.
		/// </summary>
		public TransactionStatus TransactionStatus { get; set; }
		/// <summary>
		/// The bounce message.
		/// </summary>
		public string Message { get; set; }
		/// <summary>
		/// The remote MX hostname or IP if no hostname.
		/// </summary>
		public string RemoteHostname { get; set; }
		/// <summary>
		/// The hostname of the IP address that the bounce relates to.
		/// </summary>
		public string LocalHostname { get; set; }
		/// <summary>
		/// The IP Address that the bounce relates to.
		/// </summary>
		public string LocalIpAddress { get; set; }
		/// <summary>
		/// The amount of times that this bounce has happened.
		/// </summary>
		public long Count { get; set; }
	}
}
