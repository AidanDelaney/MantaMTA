using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using Colony101.MTA.Library;
using Colony101.MTA.Library.Client;
using Colony101.MTA.Library.MtaIpAddress;
using Colony101.MTA.Library.Server;
using System.Linq;

namespace MTA_Console
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Starting " + MtaParameters.MTA_NAME);

			AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, FirstChanceExceptionEventArgs e)
			{
				Console.WriteLine(e.Exception.Message);
				Console.Write(e.Exception.StackTrace);
			};

			

			MtaIpAddressCollection ipAddresses = IpAddressManager.GetIPsForListeningOn();

			Console.WriteLine("Ports : " + string.Join(",", MtaParameters.ServerListeningPorts));
			Console.WriteLine("IPs : " + Environment.NewLine + string.Join(Environment.NewLine, from ip in ipAddresses select ip.IPAddress));

			// Array will hold all instances of SmtpServer, one for each port we will be listening on.
			ArrayList smtpServers = new ArrayList();
			
			// Create the SmtpServers
			for (int c = 0; c < ipAddresses.Count; c++)
			{
				MtaIpAddress ipAddress = ipAddresses[c];
				for (int i = 0; i < MtaParameters.ServerListeningPorts.Length; i++)
					smtpServers.Add(new SmtpServer(ipAddress.IPAddress, MtaParameters.ServerListeningPorts[i]));
			}

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
			for (int i = 0; i < smtpServers.Count; i++)
				(smtpServers[i] as SmtpServer).Dispose();
		}
	}
}
