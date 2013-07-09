using MantaMTA.Core.DAL;

namespace MantaMTA.Core.SendID
{
	public static class SendManager
	{
		/// <summary>
		/// Pause the specified send.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the send to pause.</param>
		public static void Pause(int internalSendID)
		{
			SendDB.PauseSend(internalSendID);
		}

		/// <summary>
		/// Discards a send.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		public static void Discard(int internalSendID)
		{
			SendDB.DiscardSend(internalSendID);
		}

		/// <summary>
		/// Resumes a send. Sets it to active.
		/// </summary>
		/// <param name="internalSendID">Internal ID of the Send.</param>
		public static void Resume(int internalSendID)
		{
			SendDB.ResumeSend(internalSendID);
		}
	}
}
