using System.ServiceModel;

namespace MantaMTA.Core.ServiceContracts
{
	/// <summary>
	/// Service contact for the Send Manager.
	/// </summary>
	[ServiceContract]
	public interface ISendManagerContract
	{
		/// <summary>
		/// Should pause the send.
		/// </summary>
		/// <param name="internalSendID">Sends internal ID.</param>
		[OperationContract]
		void Pause(int internalSendID);

		/// <summary>
		/// Should resume the send.
		/// </summary>
		/// <param name="internalSendID">Sends internal ID.</param>
		[OperationContract]
		void Resume(int internalSendID);

		/// <summary>
		/// Should discard the send.
		/// </summary>
		/// <param name="internalSendID">Sends internal ID.</param>
		[OperationContract]
		void Discard(int internalSendID);
	}
}
