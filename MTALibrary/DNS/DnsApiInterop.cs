using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Colony101.MTA.Library.DNS
{
	/// <summary>
	/// .Net doesn't provide deep enough access to the Windows DNS API so we need to interop are way in.
	/// </summary>
	internal class dnsapi
	{
		/// <summary>
		/// The DnsQuery function type is the generic query interface to the DNS namespace, and provides application developers with a DNS query resolution interface.
		/// http://msdn.microsoft.com/en-us/library/windows/desktop/ms682016%28v=vs.85%29.aspx
		/// </summary>
		/// <param name="pszName">A pointer to a string that represents the DNS name to query.</param>
		/// <param name="wType">A value that represents the Resource Record (RR)DNS Record Type that is queried.</param>
		/// <param name="options">A value that contains a bitmap of DNS Query Options to use in the DNS query.</param>
		/// <param name="aipServers"></param>
		/// <param name="ppQueryResults">Pointer to the results</param>
		/// <param name="pReserved"></param>
		/// <returns></returns>
		[DllImport("dnsapi", EntryPoint = "DnsQuery_W", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
		private static extern int DnsQuery_W([MarshalAs(UnmanagedType.VBByRefStr)]ref string lpstrName, QueryTypes wType, QueryOptions Options, int pExtra, ref IntPtr ppQueryResultsSet, int pReserved);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pRecordList"></param>
		/// <param name="FreeType"></param>
		[DllImport("dnsapi", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern void DnsRecordListFree(IntPtr pRecordList, int FreeType);

		/// <summary>
		/// Gets MX record collection.
		/// Each string is a single record formatted as "Priority,Host,TTL"
		/// If Null then no MX servers where found.
		/// </summary>
		/// <param name="domain">The domain to get MXs for.</param>
		/// <returns>See description.</returns>
		/// <throws>Win32 Exception. (123)</throws>
		internal static string[] GetMXRecords(string domain)
		{
			// Pointer to the first DNS result.
			IntPtr ptrFirstRecord = IntPtr.Zero;
			ArrayList results = new ArrayList();
			try
			{
				// Do the DNS query.
				int retVal = dnsapi.DnsQuery_W(ref domain, QueryTypes.DNS_TYPE_MX, QueryOptions.DNS_QUERY_STANDARD, 0, ref ptrFirstRecord, 0);

				// If the retVal isn't 0 then something went wrong
				if (retVal != 0)
				{
					// There are no DNS records of type MX.
					if (retVal == 9003 || retVal == 9501)
						return null;
					else if (retVal == 123)
						throw new DNSDomainNotFoundException();
					else
					{
						//throw new Win32Exception(retVal);
						System.Diagnostics.Trace.WriteLine("DNS_API:" + domain + " (" + retVal + ")");
						return null;
					}
				}

				MXRecord recMx = (MXRecord)Marshal.PtrToStructure(ptrFirstRecord, typeof(MXRecord)); ;
				IntPtr ptrCurrentRecord = IntPtr.Zero;
				for (ptrCurrentRecord = ptrFirstRecord; !ptrCurrentRecord.Equals(IntPtr.Zero); ptrCurrentRecord = recMx.pNext)
				{
					recMx = (MXRecord)Marshal.PtrToStructure(ptrCurrentRecord, typeof(MXRecord));
					if (recMx.wType == 15)
					{
						string line = recMx.wPreference.ToString() + "," + Marshal.PtrToStringAuto(recMx.pNameExchange) + "," + recMx.dwTtl.ToString();
						results.Add(line);
					}
				}
			}
			finally
			{
				// Always cleanup.
				DnsRecordListFree(ptrFirstRecord, 0);
			}

			return (string[])results.ToArray(typeof(string));
		}

		/// <summary>
		/// DNS Lookup query options.
		/// </summary>
		private enum QueryOptions
		{
			DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE = 1,
			DNS_QUERY_BYPASS_CACHE = 8,
			DNS_QUERY_DONT_RESET_TTL_VALUES = 0x100000,
			DNS_QUERY_NO_HOSTS_FILE = 0x40,
			DNS_QUERY_NO_LOCAL_NAME = 0x20,
			DNS_QUERY_NO_NETBT = 0x80,
			DNS_QUERY_NO_RECURSION = 4,
			DNS_QUERY_NO_WIRE_QUERY = 0x10,
			DNS_QUERY_RESERVED = -16777216,
			DNS_QUERY_RETURN_MESSAGE = 0x200,
			DNS_QUERY_STANDARD = 0,
			DNS_QUERY_TREAT_AS_FQDN = 0x1000,
			DNS_QUERY_USE_TCP_ONLY = 2,
			DNS_QUERY_WIRE_ONLY = 0x100
		}

		/// <summary>
		/// Types of MX lookup.
		/// </summary>
		private enum QueryTypes
		{
			DNS_TYPE_A = 1,
			DNS_TYPE_NS = 2,
			DNS_TYPE_CNAME = 5,
			DNS_TYPE_SOA = 6,
			DNS_TYPE_PTR = 12,
			DNS_TYPE_HINFO = 13,
			DNS_TYPE_MX = 15,
			DNS_TYPE_TXT = 16,
			DNS_TYPE_AAAA = 28
		}

		/// <summary>
		/// Represents the data for an MX record *ptr as returned by dnsapi.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct MXRecord
		{
			public IntPtr pNext;
			public string pName;
			public short wType;
			public short wDataLength;
			public int flags;
			public int dwTtl;
			public int dwReserved;
			public IntPtr pNameExchange;
			public short wPreference;
			public short Pad;
		}
	}
}
