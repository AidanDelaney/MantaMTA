using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MantaMTA.Core.Enums;

namespace WebInterfaceLib.Model
{
	public class BounceInfo
	{
		public TransactionStatus TransactionStatus { get; set; }
		public string Message { get; set; }
		public string RemoteHostname { get; set; }
		public string LocalHostname { get; set; }
		public string LocalIpAddress { get; set; }
		public int Count { get; set; }
	}
}
