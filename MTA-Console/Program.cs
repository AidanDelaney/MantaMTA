using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Server;

namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, FirstChanceExceptionEventArgs e)
			{
				Console.WriteLine(e.Exception.Message);
				Console.Write(e.Exception.StackTrace);
			};


			// The ports that will be used to listen for incoming connections.
			int[] ports = new int[] { /*25,*/ 587 };

			// Array will hold all instances of SmtpServer, one for each port.
			SmtpServer[] smtpServers = new SmtpServer[ports.Length];
			
			// Create the SmtpServers
			for (int i = 0; i < ports.Length; i++)
				smtpServers[i] = new SmtpServer(ports[i]);

			// Start the SMTP Client
			SmtpClient.Start();

			Console.WriteLine("Press 'Q' to quit");
			bool quit = false;
			while (!quit)
			{
				ConsoleKeyInfo key = Console.ReadKey(true);
				if (key.KeyChar == 'q' || key.KeyChar == 'Q')
					quit = true;
			}

			// Need to wait while servers & client shutdown.
			Console.WriteLine("Quitting...Please Wait.");
			SmtpClient.Stop();
			for (int i = 0; i < smtpServers.Length; i++)
				(smtpServers[i] as SmtpServer).Dispose();
		}
	}
}
