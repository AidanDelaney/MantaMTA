using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using MantaMTA.Core;
using MantaMTA.Core.Client;
using MantaMTA.Core.Events;
using MantaMTA.Core.MtaIpAddress;
using MantaMTA.Core.Server;

namespace MantaMTA.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			Logging.Info("MTA Started");

			AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, FirstChanceExceptionEventArgs e)
			{
				Logging.Warn("", e.Exception);
			};

			// Start the send manager service.
			MantaMTA.Core.Sends.SendManager.Instance.StartService();

			MtaIpAddressCollection ipAddresses = IpAddressManager.GetIPsForListeningOn();

			// Array will hold all instances of SmtpServer, one for each port we will be listening on.
			ArrayList smtpServers = new ArrayList();
			
			// Create the SmtpServers
			for (int c = 0; c < ipAddresses.Count; c++)
			{
				MtaIpAddress ipAddress = ipAddresses[c];
				for (int i = 0; i < MtaParameters.ServerListeningPorts.Length; i++)
					smtpServers.Add(new SmtpServer(ipAddress.IPAddress, MtaParameters.ServerListeningPorts[i]));
			}

			// Start the SMTP Client.
			MessageSender.Instance.Start();
			// Start the events (bounce/abuse) handler.
			EventsFileHandler.Instance.Start();
			
			bool quit = false;
			while (!quit)
			{
				ConsoleKeyInfo key = System.Console.ReadKey(true);
				if (key.KeyChar == 'q' || key.KeyChar == 'Q')
					quit = true;
			}

			// Need to wait while servers & client shutdown.
			MantaCoreEvents.InvokeMantaCoreStopping();
			for (int i = 0; i < smtpServers.Count; i++)
				(smtpServers[i] as SmtpServer).Dispose();

			Logging.Info("MTA Stopped");
			System.Console.WriteLine("Press any key to continue");
			System.Console.ReadKey(true);
		}
	}
}
