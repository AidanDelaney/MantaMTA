using System;
using System.Text;
using WebInterfaceLib.BO;

namespace WebInterface.Models
{
	/// <summary>
	/// Model for use by the dashboard.
	/// </summary>
	public class DashboardModel
	{
		/// <summary>
		/// Holds a summary of the transactions made in the last hour.
		/// </summary>
		public SendTransactionSummaryCollection SendTransactionSummaryCollection { get; set; }

		/// <summary>
		/// Holds the current amount of messages waiting in the queue that are not paused.
		/// </summary>
		public long Waiting { get; set; }

		/// <summary>
		/// Holds the current messages in the queue that are paused.
		/// </summary>
		public long Paused { get; set; }

		/// <summary>
		/// Information about bounces in the last hour.
		/// </summary>
		public BounceInfo[] BounceInfo { get; set; }

		/// <summary>
		/// Information about the speed of sending in the last hour.
		/// </summary>
		public SendSpeedInfo SendSpeedInfo { get; set; }

		public long RabbitMqInbound { get; set; }
		public long RabbitMqTotalOutbound { get; set; }

		public DashboardModel()
		{
			SendTransactionSummaryCollection = new SendTransactionSummaryCollection();
			Waiting = 0;
			BounceInfo = new BounceInfo[] { };
			RabbitMqInbound = 0;
			RabbitMqTotalOutbound = 0;
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
				long accepted, rejected, deferred = 0;
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
				long accepted, rejected, deferred = 0;
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
				long accepted, rejected, deferred = 0;
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
}