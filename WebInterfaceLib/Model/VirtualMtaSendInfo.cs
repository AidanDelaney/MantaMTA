
namespace WebInterfaceLib.Model
{
	/// <summary>
	/// Holds information abount a Virtual MTA and what it has done in a send.
	/// </summary>
	public class VirtualMtaSendInfo : MantaMTA.Core.MtaIpAddress.MtaIpAddress
	{
		/// <summary>
		/// The amount of messages accepted by a remote MX.
		/// </summary>
		public int Accepted { get; set; }

		/// <summary>
		/// The amount of messages rejected by a remote MX.
		/// </summary>
		public int Rejected { get; set; }

		/// <summary>
		/// The amount of attempts to send a message that where throttled by MantaMTA.
		/// </summary>
		public int Throttled { get; set; }

		/// <summary>
		/// The amount of attempts to send a message that where deferred by the remote MX.
		/// </summary>
		public int Deferred { get; set; }

		/// <summary>
		/// The amount of attempts to send messages that where made.
		/// </summary>
		private int Attempts
		{
			get
			{
				return Accepted + Deferred + Rejected + Throttled;
			}
		}

		/// <summary>
		/// The percentage of attempts that where accepted.
		/// </summary>
		public double AcceptedPercent
		{
			get
			{
				if (Attempts < 1)
					return 0;
				return (100d / Attempts) * Accepted;
			}
		}

		/// <summary>
		/// The percentage of attempts that where deferred by the remote MX.
		/// </summary>
		public double DeferredPercent
		{
			get
			{
				if (Attempts < 1)
					return 0;
				return (100d / Attempts) * Deferred;
			}
		}

		/// <summary>
		/// The percentage of attempts that where throttled by MantaMTA.
		/// </summary>
		public double ThrottledPercent
		{
			get
			{
				if (Attempts < 1)
					return 0;
				return (100d / Attempts) * Throttled;
			}
		}

		/// <summary>
		/// The percentage of attempts that where rejected by the remote MX.
		/// </summary>
		public double RejectedPercent
		{
			get
			{
				if (Attempts < 1)
					return 0;
				return (100d / Attempts) * Rejected;
			}
		}

		public VirtualMtaSendInfo()
		{
			Accepted = 0;
			Rejected = 0;
			Throttled = 0;
			Deferred = 0;
		}
	}
}
