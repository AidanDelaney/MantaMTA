using System.Collections.Generic;

namespace MantaMTA.Core
{
	public static class MantaCoreEvents
	{
		/// <summary>
		/// List of all the objects that need to be stopped.
		/// </summary>
		private static List<IStopRequired> _StopRequiredTasks = new List<IStopRequired>();

		/// <summary>
		/// Registers an instance of a class that implements IStopRequired.
		/// </summary>
		/// <param name="instance">Thing that needs to be stopped.</param>
		internal static void RegisterStopRequiredInstance(IStopRequired instance)
		{
			_StopRequiredTasks.Add(instance);
		}

		/// <summary>
		/// This should be called when the MTA is stopping as it will stop stuff that needs stopping.
		/// </summary>
		public static void InvokeMantaCoreStopping()
		{
			Logging.Debug("InvokeMantaCoreStopping Started.");

			// Always stop the Inbound Queue Manager first.
			MantaMTA.Core.Server.QueueManager.Instance.Stop();

			// Loop through the things that need stopping and stop them :)
			for (int i = 0; i < _StopRequiredTasks.Count; i++)
			{
				Logging.Debug("InvokeMantaCoreStopping > " + _StopRequiredTasks[i].GetType());
				_StopRequiredTasks[i].Stop();
			}

			Logging.Debug("InvokeMantaCoreStopping Finished.");
		}
	}
}
