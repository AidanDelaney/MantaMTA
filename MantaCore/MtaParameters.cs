using System;
using System.IO;

namespace MantaMTA.Core
{
	public class MtaParameters
	{
		/// <summary>
		/// Name of the MTA. Used in welcome banner to identify product as well as email headers.  Don't use spaces or interesting characters.
		/// </summary>
		public const string MTA_NAME = "MantaMTA";

		/// <summary>
		/// New line as should be used in emails.
		/// </summary>
		public const string NewLine = "\r\n";

		/// <summary>
		/// The time in minutes of how long stuff should be cached in memory for.
		/// </summary>
		internal const int MTA_CACHE_MINUTES = 5;

		/// <summary>
		/// Gets the ports that the SMTP server should listen for client connections on.
		/// This will almost always be 25 & 587.
		/// </summary>
		public static int[] ServerListeningPorts
		{
			get 
			{
				if (_ServerListeningPorts == null)
					_ServerListeningPorts = DAL.CfgPara.GetServerListenPorts();
				return _ServerListeningPorts;
			}
		}
		private static int[] _ServerListeningPorts { get; set; }

		/// <summary>
		/// Drop folder, for incoming messages.
		/// This should be in config.
		/// </summary>
		public static string MTA_DROPFOLDER
		{
			get
			{
				if (_MtaDropFolderLoadTime < DateTime.UtcNow)
				{
					_MtaDropFolder = DAL.CfgPara.GetDropFolder();
					Directory.CreateDirectory(_MtaDropFolder);
					_MtaDropFolderLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
				}

				return _MtaDropFolder;
			}
		}
		private static string _MtaDropFolder { get; set; }
		private static DateTime _MtaDropFolderLoadTime = DateTime.MinValue;

		/// <summary>
		/// Drop folder for abuse@
		/// </summary>
		internal static string AbuseDropFolder
		{
			get
			{
				return Path.Combine(MTA_DROPFOLDER, "abuse");
			}
		}

		/// <summary>
		/// Drop folder for email bounces.
		/// </summary>
		internal static string BounceDropFolder
		{
			get
			{
				return Path.Combine(MTA_DROPFOLDER, "bounce");
			}
		}

		/// <summary>
		/// Drop folder for feedback loop emails.
		/// </summary>
		internal static string FeedbackLoopDropFolder
		{
			get
			{
				return Path.Combine(MTA_DROPFOLDER, "feedback");
			}
		}

		/// <summary>
		/// Drop folder for postmaster@
		/// </summary>
		internal static string PostmasterDropFolder
		{
			get
			{
				return Path.Combine(MTA_DROPFOLDER, "postmaster");
			}
		}

		/// <summary>
		/// Queue folder, for messages to be sent.
		/// </summary>
		public static string MTA_QUEUEFOLDER
		{
			get
			{
				if (_MtaQueueFolderLoadTime < DateTime.UtcNow)
				{
					_MtaQueueFolder = DAL.CfgPara.GetQueueFolder();
					Directory.CreateDirectory(_MtaQueueFolder);
					_MtaQueueFolderLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
				}

				return _MtaQueueFolder;
			}
		}
		private static string _MtaQueueFolder { get; set; }
		private static DateTime _MtaQueueFolderLoadTime = DateTime.MinValue;

		/// <summary>
		/// Log foler, where SMTP Transaction logs will go.
		/// This should be in config.
		/// </summary>
		public static string MTA_SMTP_LOGFOLDER
		{
			get
			{
				if (_MtaLogFolderLoadTime < DateTime.UtcNow)
				{
					_MtaLogFolder = DAL.CfgPara.GetLogFolder();
					Directory.CreateDirectory(_MtaLogFolder);
					_MtaLogFolderLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
				}

				return _MtaLogFolder;
			}
		}
		private static string _MtaLogFolder { get; set; }
		private static DateTime _MtaLogFolderLoadTime = DateTime.MinValue;

		/// <summary>
		/// List of domains to accept messages for drop folder.
		/// All domains are toLowered!
		/// </summary>
		internal static string[] LocalDomains
		{
			get
			{
				if (_LocalDomainsLoadTime < DateTime.UtcNow)
				{
					_LocalDomains = DAL.CfgLocalDomains.GetLocalDomainsArray();
					_LocalDomainsLoadTime = DateTime.UtcNow.AddMinutes(5);
				}
				return _LocalDomains;
			}
		}
		private static string[] _LocalDomains { get; set; }
		private static DateTime _LocalDomainsLoadTime = DateTime.MinValue;

		/// <summary>
		/// The domain that return paths should use.
		/// </summary>
		public static string ReturnPathDomain
		{
			get
			{
				if (_ReturnPathDomainLoadTime < DateTime.UtcNow)
				{
					_ReturnPathDomain = DAL.CfgPara.GetReturnPathDomain();
					_ReturnPathDomainLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
				}
				return _ReturnPathDomain;
			}
		}
		private static string _ReturnPathDomain = string.Empty;
		private static DateTime _ReturnPathDomainLoadTime = DateTime.MinValue;

		/// <summary>
		/// List of IP addresses to allow relaying for.
		/// </summary>
		internal static string[] IPsToAllowRelaying
		{
			get
			{
				if (_IPsToAllowRelayingLoadTime < DateTime.UtcNow)
				{
					_IPsToAllowRelaying = DAL.CfgRelayingPermittedIP.GetRelayingPermittedIPAddresses();
					_IPsToAllowRelayingLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
				}
				return _IPsToAllowRelaying;
			}
		}
		private static string[] _IPsToAllowRelaying { get; set; }
		private static DateTime _IPsToAllowRelayingLoadTime = DateTime.MinValue;

		/// <summary>
		/// The time in minutes between send retries.
		/// </summary>
		internal static int MtaRetryInterval 
		{
			get
			{
				if (_MtaRetryIntervalLoadTime < DateTime.UtcNow)
				{
					_MtaRetryInterval = DAL.CfgPara.GetRetryIntervalMinutes();
					_MtaRetryIntervalLoadTime = DateTime.UtcNow.AddMinutes(5);
				}

				return _MtaRetryInterval;
			}
		}
		private static int _MtaRetryInterval = -1;
		private static DateTime _MtaRetryIntervalLoadTime = DateTime.MinValue;

		/// <summary>
		/// The maximum time in minutes that a message can be in the queue.
		/// </summary>
		internal static int MtaMaxTimeInQueue
		{
			get
			{
				if (_MtaMaxTimeInQueueLoadTime < DateTime.UtcNow)
				{
					_MtaMaxTimeInQueue = DAL.CfgPara.GetMaxTimeInQueueMinutes();
					_MtaMaxTimeInQueueLoadTime = DateTime.UtcNow.AddMinutes(5);
				}

				return _MtaMaxTimeInQueue;
			}
		}
		private static int _MtaMaxTimeInQueue = -1;
		private static DateTime _MtaMaxTimeInQueueLoadTime = DateTime.MinValue;

		internal static class Client
		{
			/// <summary>
			/// Port for SMTP connections by the client to remote servers when sending
			/// messages. This will likely only every change when developing/debugging.
			/// </summary>
			public const int SMTP_PORT = 25;

			/// <summary>
			/// The time in seconds after which an active but idle connection should be
			/// considered timed out.
			/// </summary>
			public static int ConnectionIdleTimeoutInterval
			{
				get
				{
					if (_ConnectionIdleTimeoutIntervalLoadTime < DateTime.UtcNow)
					{
						_ConnectionIdleTimeoutInterval = DAL.CfgPara.GetClientIdleTimeout();
						_ConnectionIdleTimeoutIntervalLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
					}

					return _ConnectionIdleTimeoutInterval;
				}
			}
			private static int _ConnectionIdleTimeoutInterval = -1;
			private static DateTime _ConnectionIdleTimeoutIntervalLoadTime = DateTime.MinValue;

			/// <summary>
			/// The time in seconds for connection read timeouts.
			/// </summary>
			public static int ConnectionReceiveTimeoutInterval
			{
				get 
				{
					if (_ConnectionReceiveTimeoutIntervalLoadTime < DateTime.UtcNow)
					{
						_ConnectionReceiveTimeoutInterval = DAL.CfgPara.GetReceiveTimeout();
						_ConnectionReceiveTimeoutIntervalLoadTime = _ConnectionReceiveTimeoutIntervalLoadTime.AddMinutes(MTA_CACHE_MINUTES);
					}

					return _ConnectionReceiveTimeoutInterval;
				}
			}
			public static int _ConnectionReceiveTimeoutInterval = -1;
			private static DateTime _ConnectionReceiveTimeoutIntervalLoadTime = DateTime.MinValue;


			/// <summary>
			/// The time in seconds for connection send timeouts.
			/// </summary>
			public static int ConnectionSendTimeoutInterval
			{
				get 
				{
					if (_connectionSendTimeoutIntervalLoadTime < DateTime.UtcNow)
					{
						_connectionSendTimeoutInterval = DAL.CfgPara.GetSendTimeout();
						_connectionSendTimeoutIntervalLoadTime = DateTime.UtcNow.AddMinutes(MTA_CACHE_MINUTES);
					}

					return _connectionSendTimeoutInterval;
				}
			}
			private static int _connectionSendTimeoutInterval = -1;
			private static DateTime _connectionSendTimeoutIntervalLoadTime = DateTime.MinValue;
		}

		/// <summary>
		/// The amount of days to keep SMTP logs for before deleting them.
		/// </summary>
		internal static int DaysToKeepSmtpLogsFor
		{
			get
			{
				if (_DaysToKeepSmtpLogsFor == -1)
					_DaysToKeepSmtpLogsFor = DAL.CfgPara.GetDaysToKeepSmtpLogsFor();
				return _DaysToKeepSmtpLogsFor;
			}
		}
		private static int _DaysToKeepSmtpLogsFor = -1;

		/// <summary>
		/// The URL to post Manta Events (abuse/bounce) to.
		/// </summary>
		public static Uri EventForwardingHttpPostUrl
		{
			get
			{
				if (string.IsNullOrEmpty(_EventForwardingHttpPostUrl))
					_EventForwardingHttpPostUrl = DAL.CfgPara.GetEventForwardingHttpPostUrl();

				return new Uri(_EventForwardingHttpPostUrl);
			}
		}
		private static string _EventForwardingHttpPostUrl = string.Empty;
	}

	/// <summary>
	/// Should be thrown when a Send is in a discarding state and an attempt is made to queue a message to it.
	/// </summary>
	public class SendDiscardingException : Exception { }

	/// <summary>
	/// Exception is thrown when an email is picked up for sending but there are no connections avalible and
	/// cannot attempt to create another as we've hit the maximum.
	/// </summary>
	public class MaxConnectionsException : Exception { }
}
