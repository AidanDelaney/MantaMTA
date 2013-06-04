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
		/// </summary>
		internal static List<string> LocalDomains
		{
			get
			{
				List<string> tmp = new List<string>();
				tmp.Add("local");
				tmp.Add("localhost");
				return tmp;
			}
		}

		/// <summary>
		/// List of IP addresses to allow relaying for.
		/// </summary>
		internal static List<string> IPsToAllowRelaying
		{
			get
			{
				List<string> tmp = new List<string>();
				tmp.Add("127.0.0.1");
				tmp.Add("10.173.10.11");
				return tmp;
			}
		}
	}
}
