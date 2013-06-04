using System;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Server;


namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			SmtpServer smtpServer = new SmtpServer(25);
			SmtpClient.Start();

			Console.WriteLine("Press any key to quit");
			Console.ReadKey(true);
		}
	}
}
