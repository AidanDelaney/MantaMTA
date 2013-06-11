using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Colony101.MTA.Library.MtaIpAddress
{
	/// <summary>
	/// Extends the .Net IPAddress class with extra Colony101 MTA bits.
	/// </summary>
	public class MtaIpAddress
	{
		/// <summary>
		/// 
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// The Hostname of specified for this IP Address.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public IPAddress IPAddress {get;set;} 

		/// <summary>
		/// If true the IP address can be used for receiving. 
		/// </summary>
		public bool IsSmtpInbound { get; set; }

		/// <summary>
		/// If true the IP address can be used for sending.
		/// </summary>
		public bool IsSmtpOutbound { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public TcpClient CreateTcpClient(int port)
		{
			return new TcpClient(new IPEndPoint(this.IPAddress, port));
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class MtaIpAddressCollection : List<MtaIpAddress>
	{
		public MtaIpAddressCollection() { }
		public MtaIpAddressCollection(IEnumerable<MtaIpAddress> collection) : base(collection) { }
	}
}
