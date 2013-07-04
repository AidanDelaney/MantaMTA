using System;
using MantaMTA.Core.Client.BO;

namespace MantaMTA.Core.Message
{
	public static class ReturnPathManager
	{
		private const string RCPT_TO_AT_REPLACEMENT = "=";


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rcptTo"></param>
		/// <param name="internalSendID"></param>
		/// <param name="mailFrom"></param>
		/// <returns></returns>
		public static string GenerateReturnPath(string rcptTo, int internalSendID)
		{

			return string.Format("return-{0}-{1}@{2}",
						rcptTo.Replace("@", RCPT_TO_AT_REPLACEMENT),
						internalSendID.ToString("X"),
						MtaParameters.ReturnPathDomain);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rcptTo"></param>
		/// <param name="internalSendID"></param>
		/// <returns></returns>
		public static bool TryDecode(string returnPath, out string rcptTo, out int internalSendID)
		{
			try
			{
				returnPath = returnPath.Substring("return-".Length, returnPath.LastIndexOf("@") - "return-".Length);
				internalSendID = Int32.Parse(returnPath.Substring(returnPath.LastIndexOf("-") + 1), System.Globalization.NumberStyles.HexNumber);
				rcptTo = returnPath.Substring(0, returnPath.LastIndexOf("-")).Replace(RCPT_TO_AT_REPLACEMENT, "@");
				return true;
			}
			catch (Exception)
			{
				rcptTo = null;
				internalSendID = -1;
				return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="messageID"></param>
		/// <returns></returns>
		public static string GetReturnPathFromMessageID(Guid messageID)
		{
			MtaMessage msg = DAL.MtaMessageDB.GetMtaMessage(messageID);
			if (msg == null)
				return string.Empty;

			return msg.MailFrom.ToString();
		}
	}
}
