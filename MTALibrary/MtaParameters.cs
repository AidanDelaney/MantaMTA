using System.Collections.Generic;
using System.IO;

namespace Colony101.MTA.Library
{
	internal class MtaParameters
	{
		/// <summary>
		/// Name of the MTA. Used in welcome banner to identify product.
		/// </summary>
		internal const string MTA_NAME = "Colony101-MTA";

		/// <summary>
		/// Drop folder, for incoming messages.
		/// This should be in config.
		/// </summary>
		internal static string MTA_DROPFOLDER
		{
			get
			{
				string path = @"c:\temp\drop\";
				Directory.CreateDirectory(path);
				return path;
			}
		}

		/// <summary>
		/// Queue folder, for messages to be sent.
		/// </summary>
		internal static string MTA_QUEUEFOLDER
		{
			get
			{
				string path = @"c:\temp\queue\";
				Directory.CreateDirectory(path);
				return path;
			}
		}

		/// <summary>
		/// Log foler, where SMTP Transaction logs will go.
		/// This should be in config.
		/// </summary>
		internal static string MTA_LOGFOLDER
		{
			get
			{
				string path = @"c:\temp\logs\";
				Directory.CreateDirectory(path);
				return path;
			}
		}

		/// <summary>
		/// List of domains to accept messages for drop folder.
		/// All domains are toLowered!
		/// </summary>
		internal static string[] LocalDomains
		{
			get
			{
				if (_LocalDomains == null)
					_LocalDomains = DAL.CfgLocalDomains.GetLocalDomainsArray();
				return _LocalDomains;
			}
		}
		private static string[] _LocalDomains { get; set; }

		/// <summary>
		/// List of IP addresses to allow relaying for.
		/// </summary>
		internal static string[] IPsToAllowRelaying
		{
			get
			{
				if (_IPsToAllowRelaying == null)
					_IPsToAllowRelaying = DAL.cfgRelayingPermittedIP.GetRelayingPermittedIPAddresses();
				return _IPsToAllowRelaying;
			}
		}
		private static string[] _IPsToAllowRelaying { get; set; }
	}
}
