using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebInterfaceLib.Model;

namespace WebInterface.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class SendsModel
	{
		public SendInfoCollection SendInfo { get; set; }
		public int PageCount { get; set; }
		public int CurrentPage { get; set; }

		public SendsModel(SendInfoCollection sendInfo, int currentPage, int pageCount)
		{
			this.SendInfo = sendInfo;
			this.PageCount = pageCount;
			this.CurrentPage = currentPage;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendReportBase
	{
		public string SendID { get; set; }
		public SendReportBase(string sendID)
		{
			this.SendID = sendID;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendReportOverview : SendReportBase
	{
		public SendInfo SendInfo { get; set; }
		public SendReportOverview(SendInfo sendInfo) : base(sendInfo.ID)
		{
			this.SendInfo = sendInfo;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendReportVirtualMta : SendReportBase
	{
		public VirtualMtaSendInfo[] VirtualMtaSendInfo { get; set; }
		public SendReportVirtualMta(VirtualMtaSendInfo[] vmtaInfo, string sendID) : base(sendID)
		{
			this.VirtualMtaSendInfo = vmtaInfo;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendReportBounces : SendReportBase
	{
		public BounceInfo[] BounceInfo { get; set; }
		public int PageCount { get; set; }
		public int CurrentPage { get; set; }
		public SendReportBounces(BounceInfo[] bounceInfo, string sendID, int currentPage, int pageCount) : base(sendID)
		{
			this.BounceInfo = bounceInfo;
			this.PageCount = pageCount;
			this.CurrentPage = currentPage;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendReportSpeed : SendReportBase
	{
		public SendSpeedInfo SendSpeedInfo { get; set; }
		public SendReportSpeed(SendSpeedInfo info, string sendID) : base(sendID)
		{
			SendSpeedInfo = info;
		}

		public string GetAcceptedSendRates()
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (DateTime timestamp in SendSpeedInfo.Dates)
			{
				int accepted, rejected, deferred = 0;
				SendSpeedInfo.GetDataPoints(timestamp, out accepted, out rejected, out deferred);
				if (!first)
					sb.Append(",");
				else
					first = false;

				sb.Append(accepted);
			}

			return sb.ToString();
		}

		//
		public string GetRejectedSendRates()
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (DateTime timestamp in SendSpeedInfo.Dates)
			{
				int accepted, rejected, deferred = 0;
				SendSpeedInfo.GetDataPoints(timestamp, out accepted, out rejected, out deferred);
				if (!first)
					sb.Append(",");
				else
					first = false;

				sb.Append(rejected);
			}

			return sb.ToString();
		}

		public string GetDeferredSendRates()
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (DateTime timestamp in SendSpeedInfo.Dates)
			{
				int accepted, rejected, deferred = 0;
				SendSpeedInfo.GetDataPoints(timestamp, out accepted, out rejected, out deferred);
				if (!first)
					sb.Append(",");
				else
					first = false;

				sb.Append(deferred);
			}

			return sb.ToString();
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class SendStatusUpdated
	{
		public string RedirectUrl { get; set; }
		public SendStatusUpdated(string redirectUrl)
		{
			RedirectUrl = redirectUrl;
		}
	}
}