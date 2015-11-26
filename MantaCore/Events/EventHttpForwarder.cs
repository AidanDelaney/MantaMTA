using System;
using System.Data.SqlTypes;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MantaMTA.Core.Events
{
	public class EventHttpForwarder : IStopRequired
	{
		private static EventHttpForwarder _Instance = new EventHttpForwarder();
		public static EventHttpForwarder Instance { get { return _Instance; } }
		private EventHttpForwarder() { }

		/// <summary>
		/// Will be set to true when MTA is stopping.
		/// </summary>
		private volatile bool _IsStopping = false;

		/// <summary>
		/// Should be set to true when processing events and false when done.
		/// </summary>
		private bool _IsRunning = false;

		/// <summary>
		/// IStopRequired method. Will stop the EventHttpForwarder when the MTA is stopping.
		/// </summary>
		public void Stop()
		{
			_IsStopping = true;
			// Wait until EventHttpForwarder has stopped.
			while (_IsRunning)
				Thread.Sleep(50);
		}

		/// <summary>
		/// Call this method to start the EventHttpForwarder.
		/// </summary>
		public void Start()
		{
			MantaCoreEvents.RegisterStopRequiredInstance(this);
			Thread t = new Thread(new ThreadStart(ForwardEvents));
			t.IsBackground = true;
			t.Start();
		}

		/// <summary>
		/// Does the actual forwarding of the events.
		/// </summary>
		private void ForwardEvents()
		{
			_IsRunning = true;

			try
			{
				// Keep looping as long as the MTA is running.
				while (!_IsStopping)
				{
					MantaEventCollection events = null;
					// Get events for forwarding.
					try
					{
						events = Core.DAL.EventDB.GetEventsForForwarding(10);
					}
					catch (SqlNullValueException)
					{
						events = new MantaEventCollection();
					}


					if (events.Count == 0)
					{
						// No events to forward sleep for a second and look again.
						Thread.Sleep(1000);
						continue;
					}
					else
					{
						// Found events to forward, create and run Tasks to forward.
						var eventTasks = new Task[events.Count];
						for (var i = 0; i < events.Count; i++)
							ForwardEventAsync(events[i]).Wait();
						// eventTasks[0] = Task.Factory.StartNew(async (evt) => await ForwardEventAsync((MantaEvent)evt), events[i]);

						//Task.WaitAll(eventTasks);
					}
				}
			}
			catch (Exception ex)
			{
				// Something went wrong.
				Logging.Error("EventHttpForwarder encountered an error.", ex);
				MantaCoreEvents.InvokeMantaCoreStopping();
				Environment.Exit(-1);
			}

			_IsRunning = false;
		}

		private async Task ForwardEventAsync(MantaEvent evt)
		{
			try
			{
				if (_IsStopping)
					return;

				// Create the HTTP POST request to the remove endpoint.
				var httpRequest = (HttpWebRequest)WebRequest.Create(MtaParameters.EventForwardingHttpPostUrl);
				httpRequest.Method = "POST";
				httpRequest.ContentType = "text/json";

				// Convert the Event to JSON.
				string eventJson = string.Empty;
				switch (evt.EventType)
				{
					case MantaEventType.Abuse:
						eventJson = new JavaScriptSerializer().Serialize((MantaAbuseEvent)evt);
						break;
					case MantaEventType.Bounce:
						eventJson = new JavaScriptSerializer().Serialize((MantaBounceEvent)evt);
						break;
					case MantaEventType.TimedOutInQueue:
						eventJson = new JavaScriptSerializer().Serialize((MantaTimedOutInQueueEvent)evt);
						break;
					default:
						eventJson = new JavaScriptSerializer().Serialize(evt);
						break;
				}

				// Remove the forwarded property as it is internal only.
				eventJson = Regex.Replace(eventJson, ",\"Forwarded\":(false|true)", string.Empty);

				// Write the event json to the POST body.
				using (StreamWriter writer = new StreamWriter(await httpRequest.GetRequestStreamAsync()))
				{
					await writer.WriteAsync(eventJson);
				}

				// Send the POST and get the response.
				HttpWebResponse httpResponse = (HttpWebResponse)await httpRequest.GetResponseAsync();

				// Get the response body.
				string responseBody = string.Empty;
				using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
				{
					responseBody = await reader.ReadToEndAsync();
				}

				// If response body is just a "." then event was received successfully.
				if (responseBody.Trim().StartsWith("."))
				{
					// Log that the event forwared.
					evt.Forwarded = true;
					await EventsManager.Instance.SaveAsync(evt);
				}
			}
			catch (Exception ex)
			{
				// We failed to forward the event. Most likly because the remote server didn't respond.
				Logging.Error("Failed to forward event " + evt.ID, ex);
			}
		}
	}
}
