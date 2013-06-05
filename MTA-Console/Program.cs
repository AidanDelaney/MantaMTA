using System;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Server;


namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			int[] ports = new int[] { 25, 587 };
			for (int i = 0; i < ports.Length; i++)
			{
				SmtpServer smtpServer = new SmtpServer(ports[i]);
				Console.WriteLine("Started Server on " + ports[i]);
			}
			SmtpClient.Start();

			Console.WriteLine("Press any key to quit");
			Console.ReadKey(true);
		}
	}
}
