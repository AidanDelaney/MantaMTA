using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MantaMTA.Core.VirtualMta
{
	/// <summary>
	/// Extends the .Net IPAddress class with extra Colony101 MTA bits.
	/// </summary>
	public class VirtualMTA
	{
		/// <summary>
		/// ID of the Virtual MTA.
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// The Hostname of specified for this IP Address.
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// The .Net IP Address object for this Virtual MTA.
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
		/// Holds the information regarding how often this IP has been used. 
		/// This is used for load balancing the IPs in a pool, it is never saved to the database.
		/// string = mx record hostname (lowercase).
		/// int = count of times used.
		/// </summary>
		internal ConcurrentDictionary<string, int> SendsCounter = new ConcurrentDictionary<string, int>();

		/// <summary>
		/// Creates a new TcpClient for this Virtual MTA.
		/// </summary>
		/// <param name="port"></param>
		/// <returns></returns>
		public TcpClient CreateTcpClient(int port)
		{
			return new TcpClient(new IPEndPoint(this.IPAddress, port));
		}

		/// <summary>
		/// Constructor sets defaults.
		/// </summary>
		public VirtualMTA()
		{
			this.Hostname = string.Empty;
			this.ID = 0;
			this.IPAddress = null;
			this.IsSmtpInbound = true;
			this.IsSmtpOutbound = true;
		}
	}

	/// <summary>
	/// A Collection a Virtual MTA's.
	/// </summary>
	public class VirtualMTACollection : List<VirtualMTA>
	{
		public VirtualMTACollection() { }
		public VirtualMTACollection(IEnumerable<VirtualMTA> collection) : base(collection) { }
	}
}
