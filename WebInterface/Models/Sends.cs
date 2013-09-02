using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using WebInterfaceLib.Model;

namespace WebInterface.Models
{
	/// <summary>
	/// The Model for the Sends & Queue page.
	/// </summary>
	public class SendsModel
	{
		/// <summary>
		/// Collection of information about each send.
		/// </summary>
		public SendInfoCollection SendInfo { get; set; }
		/// <summary>
		/// Count of the amount of pages that are required to view all of the data.
		/// </summary>
		public int PageCount { get; set; }
		/// <summary>
		/// The number of the current page.
		/// </summary>
		public int CurrentPage { get; set; }

		public SendsModel(SendInfoCollection sendInfo, int currentPage, int pageCount)
		{
			this.SendInfo = sendInfo;
			this.PageCount = pageCount;
			this.CurrentPage = currentPage;
		}
	}

	/// <summary>
	/// Base class required for all Send Report pages.
	/// </summary>
	public class SendReportBase
	{
		/// <summary>
		/// ID of the Send that the report is for.
		/// </summary>
		public string SendID { get; set; }

		public SendReportBase(string sendID)
		{
			this.SendID = sendID;
		}
	}

	/// <summary>
	/// Model for the Send Report Overview Page
	/// </summary>
	public class SendReportOverview : SendReportBase
	{
		/// <summary>
		/// Information about the send.
		/// </summary>
		public SendInfo SendInfo { get; set; }

		public SendReportOverview(SendInfo sendInfo) : base(sendInfo.ID)
		{
			this.SendInfo = sendInfo;
		}
	}

	/// <summary>
	/// Model for the Virtual MTA Send Report page.
	/// </summary>
	public class SendReportVirtualMta : SendReportBase
	{
		/// <summary>
		/// Array of send information for Virtual MTAs
		/// </summary>
		public VirtualMtaSendInfo[] VirtualMtaSendInfo { get; set; }

		public SendReportVirtualMta(VirtualMtaSendInfo[] vmtaInfo, string sendID) : base(sendID)
		{
			this.VirtualMtaSendInfo = vmtaInfo;
		}
	}

	/// <summary>
	/// Model for the Send Bounces Report page.
	/// </summary>
	public class SendReportBounces : SendReportBase
	{
		/// <summary>
		/// Array of information about bounces for this send.
		/// </summary>
		public BounceInfo[] BounceInfo { get; set; }
		/// <summary>
		/// Total number of pages required to view all information 
		/// about bounces for this send.
		/// </summary>
		public int PageCount { get; set; }
		/// <summary>
		/// The current page of bounce data.
		/// </summary>
		public int CurrentPage { get; set; }

		public SendReportBounces(BounceInfo[] bounceInfo, string sendID, int currentPage, int pageCount) : base(sendID)
		{
			this.BounceInfo = bounceInfo;
			this.PageCount = pageCount;
			this.CurrentPage = currentPage;
		}
	}

	/// <summary>
	/// Model for the Send Speed Report.
	/// </summary>
	public class SendReportSpeed : SendReportBase
	{
		/// <summary>
		/// The send speed data.
		/// </summary>
		public SendSpeedInfo SendSpeedInfo { get; set; }

		public SendReportSpeed(SendSpeedInfo info, string sendID) : base(sendID)
		{
			SendSpeedInfo = info;
		}

		/// <summary>
		/// Gets the Accepted Send Rates for chart.
		/// </summary>
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

		/// <summary>
		/// Gets the Rejected Send Rates for chart.
		/// </summary>
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

		/// <summary>
		/// Gets the Deferred Send Rates for chart.
		/// </summary>
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
	/// Model for the Send Status Updated Page.
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