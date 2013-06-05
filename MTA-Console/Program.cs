using System;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Server;


namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			// The ports that will be used to listen for incoming connections.
			int[] ports = new int[] { 25, 587 };

			// Array will hold all instances of SmtpServer, one for each port.
			SmtpServer[] smtpServers = new SmtpServer[ports.Length];
			
			// Create the SmtpServers
			for (int i = 0; i < ports.Length; i++)
				smtpServers[i] = new SmtpServer(ports[i]);

			// Start the SMTP Client
			SmtpClient.Start();

			Console.WriteLine("Press any key to quit");
			Console.ReadKey(true);

			// Need to wait while servers & client shutdown.
			Console.WriteLine("Quiting...Please Wait.");
			SmtpClient.Stop();
			for (int i = 0; i < smtpServers.Length; i++)
				(smtpServers[i] as SmtpServer).Dispose();
		}
	}
}
