using System;
using System.Collections;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.Server;


namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			ArrayList smtpServers = new ArrayList();
			int[] ports = new int[] {/* 25,*/ 587 };
			for (int i = 0; i < ports.Length; i++)
				smtpServers.Add(new SmtpServer(ports[i]));
			SmtpClient.Start();

			Console.WriteLine("Press any key to quit");
			Console.ReadKey(true);

			Console.WriteLine("Quiting...Please Wait.");
			SmtpClient.Stop();
			for (int i = 0; i < smtpServers.Count; i++)
				(smtpServers[i] as SmtpServer).Dispose();
		}
	}
}
