using System.ServiceProcess;

namespace MantaService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			ServiceBase.Run(new MantaMTA());
		}
	}
}
