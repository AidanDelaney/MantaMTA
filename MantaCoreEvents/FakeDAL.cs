using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MantaMTA.Core.Events;
using MantaMTA.Core.Events.Enums;

namespace MantaMTA.Core.DAL
{
	internal static class CfgPara
	{
		internal static int[] GetServerListenPorts()
		{
			return new int[] { 25 };
		}

		internal static string GetDropFolder()
		{
			return @"C:\temp\Manta\Drop\";
		}

		internal static string GetQueueFolder()
		{
			return @"C:\temp\Manta\Queue\";
		}

		internal static string GetLogFolder()
		{
			return @"C:\temp\Manta\Log\";
		}

		internal static int GetRetryIntervalMinutes()
		{
			return 1;
		}

		internal static int GetMaxTimeInQueueMinutes()
		{
			return 1;
		}

		internal static int GetClientIdleTimeout()
		{
			return 1;
		}

		internal static int GetReceiveTimeout()
		{
			return 1;
		}

		internal static int GetSendTimeout()
		{
			return 1;
		}
	}

	internal static class CfgLocalDomains
	{
		internal static string[] GetLocalDomainsArray()
		{
			return new string[] { "snt0.net" };
		}
	}

	internal static class CfgRelayingPermittedIP
	{
		internal static string[] GetRelayingPermittedIPAddresses()
		{
			return new string[] { "127.0.0.1" };
		}
	}


	internal static class SendDb
	{
		internal static string GetSendIdFromInternalSendId(int internalSendId)
		{
			return "Ben's Email";
		}
	}
}
