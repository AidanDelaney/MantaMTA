using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using MantaMTA.Core.DAL;
using MantaMTA.Core.VirtualMta;
using WebInterfaceLib.BO;

namespace WebInterfaceLib.DAL
{
	public static class VirtualMtaDB
	{
		/// <summary>
		/// Gets information about VirtualMTA sends for the specified send.
		/// </summary>
		/// <param name="sendID">ID of the send to get information for.</param>
		/// <returns>Information about the usage of each VirtualMTA in the send.</returns>
		public static VirtualMtaSendInfo[] GetSendVirtualMTAStats(string sendID)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
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

		/// <summary>
		/// Creates a VirtualMtaSendInfo object and fills it with data from the data record.
		/// </summary>
		/// <param name="record">Record to get the data from.</param>
		/// <returns>A VirtualMtaSendInfo object filled with data from the data record.</returns>
		public static VirtualMtaSendInfo CreateAndFillVirtualMtaSendInfo(IDataRecord record)
		{
			VirtualMtaSendInfo vinfo = new VirtualMtaSendInfo();

			vinfo.ID = record.GetInt32("ip_ipAddress_id");
			vinfo.Hostname = record.GetString("ip_ipAddress_hostname");
			vinfo.IPAddress = System.Net.IPAddress.Parse(record.GetString("ip_ipAddress_ipAddress"));
			vinfo.IsSmtpInbound = record.GetBoolean("ip_ipAddress_isInbound");
			vinfo.IsSmtpOutbound = record.GetBoolean("ip_ipAddress_isOutbound");
			vinfo.Accepted = record.GetInt64("Accepted");
			vinfo.Deferred = record.GetInt64("Deferred");
			vinfo.Rejected = record.GetInt64("Rejected");
			vinfo.Throttled = record.GetInt64("Throttled");

			return vinfo;
		}

		/// <summary>
		/// Save the specified Virtual MTA to the Database.
		/// </summary>
		/// <param name="vmta"></param>
		public static void Save(VirtualMTA vmta)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
IF EXISTS(SELECT 1 FROM man_ip_ipAddress WHERE ip_ipAddress_id = @id)
	BEGIN
		UPDATE man_ip_ipAddress
		SET ip_ipAddress_ipAddress = @ipAddress,
			ip_ipAddress_hostname = @hostname,
			ip_ipAddress_isInbound = @isInbound,
			ip_ipAddress_isOutbound = @isOutbound
		WHERE ip_ipAddress_id = @id
	END
ELSE
	BEGIN
		INSERT INTO man_ip_ipAddress(ip_ipAddress_ipAddress, ip_ipAddress_hostname, ip_ipAddress_isInbound, ip_ipAddress_isOutbound)
		VALUES(@ipAddress, @hostname, @isInbound, @isOutbound)
	END
";
				cmd.Parameters.AddWithValue("@id", vmta.ID);
				cmd.Parameters.AddWithValue("@ipAddress", vmta.IPAddress.ToString());
				cmd.Parameters.AddWithValue("@hostname", vmta.Hostname);
				cmd.Parameters.AddWithValue("@isInbound", vmta.IsSmtpInbound);
				cmd.Parameters.AddWithValue("@isOutbound", vmta.IsSmtpOutbound);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Deletes the specified Virtual MTA from the Database.
		/// </summary>
		/// <param name="id">ID of Virtual MTA to Delete.</param>
		public static void Delete(int id)
		{
			using (SqlConnection conn = MantaDB.GetSqlConnection())
			{
				SqlCommand cmd = conn.CreateCommand();
				cmd.CommandText = @"
DELETE FROM man_ip_ipAddress WHERE ip_ipAddress_id = @id
DELETE FROM man_ip_groupMembership WHERE ip_ipAddress_id = @id";
				cmd.Parameters.AddWithValue("@id", id);
				conn.Open();
				cmd.ExecuteNonQuery();
			}
		}
	}
}
