using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MantaMTA.Core.Events
{
	/// <summary>
	/// Handles events such as Abuse and Bounces as a result of emails being sent.
	/// </summary>
	internal class EventsManager
	{
		/// <summary>
		/// Holds a singleton instance of the EventsManager.
		/// </summary>
		public static EventsManager Instance { get { return _Instance; } }
		private static readonly EventsManager _Instance = new EventsManager();
		private EventsManager() { }


		/// <summary>
		/// Examines a string to identify detailed bounce information from it.
		/// </summary>
		/// <param name="message">Could either be an entire email or just a multiple line response from another MTA.</param>
		/// <returns>A EmailProcessingResult value indicating the result.</returns>
		public EmailProcessingResult ProcessBounce(string message)
		{
			// Get some values from the emailContent.
			
			// return ProcessBounce(from, to, message);
			return EmailProcessingResult.Unknown;
		}


		/// <summary>
		/// Examines a string to identify detailed bounce information from it.
		/// </summary>
		/// <param name="emailContent"></param>
		public EmailProcessingResult ProcessFeedbackLoop(string message)
		{
			/*
			 
			abus@manta.io
			
			 
			*/


			// Check the values found in the report.  Things like OriginalSender and RecipientEmail.


			// Check return-path indicates it's for us.

			return EmailProcessingResult.Unknown;
		}
	}
}
