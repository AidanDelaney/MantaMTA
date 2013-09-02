
namespace WebInterfaceLib.Model
{
	public class VirtualMtaSendInfo : MantaMTA.Core.MtaIpAddress.MtaIpAddress
	{
		public int Accepted { get; set; }
		public int Rejected { get; set; }
		public int Throttled { get; set; }
		public int Deferred { get; set; }

		/// <summary>
		/// 
		/// </summary>
		private int Attempts
		{
			get
			{
				return Accepted + Deferred + Rejected + Throttled;
			}
		}

		/// <summary>
		/// 
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
		/// 
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
		/// 
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
		/// 
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
