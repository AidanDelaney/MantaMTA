using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MantaMTA.Core.DAL;
using WebInterfaceLib.Model;

namespace WebInterfaceLib.DAL
{
	public static class VirtualMTA
	{
		public static VirtualMtaSendInfo[] GetSendVirtualMTAStats(string sendID)
		{
			using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString))
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
--// Get the internal Send ID
DECLARE @internalSendId int
SELECT @internalSendId = [snd].mta_send_internalId
FROM man_mta_send as [snd]
WHERE [snd].mta_send_id = @sndID

DECLARE @usedIpAddressIds table(ip_ipAddress_id int)
--// Get the IP addresses used by the send
INSERT INTO @usedIpAddressIds
SELECT DISTINCT(ip_ipAddress_id)
FROM man_mta_transaction as [tran]
JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id
WHERE [msg].mta_send_internalId = @internalSendId

--// Get the actual data
SELECT [ip].*,
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [tran].ip_ipAddress_id = [ip].ip_ipAddress_id AND [msg].mta_send_internalId = @internalSendId AND [tran].mta_transactionStatus_id = 4) AS 'Accepted',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [tran].ip_ipAddress_id = [ip].ip_ipAddress_id AND [msg].mta_send_internalId = @internalSendId AND ([tran].mta_transactionStatus_id = 2 OR [tran].mta_transactionStatus_id = 3 OR [tran].mta_transactionStatus_id = 6)) AS 'Rejected',	
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [tran].ip_ipAddress_id = [ip].ip_ipAddress_id AND [msg].mta_send_internalId = @internalSendId AND [tran].mta_transactionStatus_id = 5) AS 'Throttled',
	(SELECT COUNT(*) FROM man_mta_transaction as [tran] JOIN man_mta_msg as [msg] ON [tran].mta_msg_id = [msg].mta_msg_id WHERE [tran].ip_ipAddress_id = [ip].ip_ipAddress_id AND [msg].mta_send_internalId = @internalSendId AND [tran].mta_transactionStatus_id = 1) AS 'Deferred'
FROM man_ip_ipAddress as [ip]
WHERE [ip].ip_ipAddress_id IN (SELECT * FROM @usedIpAddressIds)";
				cmd.Parameters.AddWithValue("@sndID", sendID);
				return DataRetrieval.GetCollectionFromDatabase<VirtualMtaSendInfo>(cmd, CreateAndFillVirtualMtaSendInfo).ToArray();
			}
		}

		public static VirtualMtaSendInfo CreateAndFillVirtualMtaSendInfo(IDataRecord record)
		{
			VirtualMtaSendInfo vinfo = new VirtualMtaSendInfo();

			vinfo.ID = record.GetInt32("ip_ipAddress_id");
			vinfo.Hostname = record.GetString("ip_ipAddress_hostname");
			vinfo.IPAddress = System.Net.IPAddress.Parse(record.GetString("ip_ipAddress_ipAddress"));
			vinfo.IsSmtpInbound = record.GetBoolean("ip_ipAddress_isInbound");
			vinfo.IsSmtpOutbound = record.GetBoolean("ip_ipAddress_isOutbound");
			vinfo.Accepted = record.GetInt32("Accepted");
			vinfo.Deferred = record.GetInt32("Deferred");
			vinfo.Rejected = record.GetInt32("Rejected");
			vinfo.Throttled = record.GetInt32("Throttled");

			return vinfo;
		}
	}
}
