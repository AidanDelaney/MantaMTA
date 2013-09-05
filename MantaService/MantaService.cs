using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.ServiceProcess;
using MantaMTA.Core;
using MantaMTA.Core.Client;
using MantaMTA.Core.Events;
using MantaMTA.Core.VirtualMta;
using MantaMTA.Core.Sends;
using MantaMTA.Core.Server;

namespace MantaService
{
	public partial class MantaMTA : ServiceBase
	{
		// Array will hold all instances of SmtpServer, one for each port we will be listening on.
		private List<SmtpServer> _SmtpServers = new List<SmtpServer>();

		public MantaMTA()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			Logging.Info("Starting Manta MTA Service.");

			/*AppDomain.CurrentDomain.FirstChanceException += delegate(object sender, FirstChanceExceptionEventArgs e)
			{
				Logging.Debug("", e.Exception);
			};*/

			AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
			{
				Exception ex = (Exception)e.ExceptionObject;
				Logging.Fatal(ex.Message, ex);
			};

			// Start the send manager service.
			SendManager.Instance.StartService();

			VirtualMTACollection ipAddresses = VirtualMtaManager.GetVirtualMtasForListeningOn();
			
			// Create the SmtpServers
			for (int c = 0; c < ipAddresses.Count; c++)
			{
				VirtualMTA ipAddress = ipAddresses[c];
				for (int i = 0; i < MtaParameters.ServerListeningPorts.Length; i++)
					_SmtpServers.Add(new SmtpServer(ipAddress.IPAddress, MtaParameters.ServerListeningPorts[i]));
			}

			// Start the SMTP Client.
			MessageSender.Instance.Start();
			// Start the events (bounce/abuse) handler.
			EventsFileHandler.Instance.Start();

			Logging.Info("Manta MTA Service has started.");
		}

		protected override void OnStop()
		{
			Logging.Info("Stopping Manta MTA Service");

			// Need to wait while servers & client shutdown.
			MantaCoreEvents.InvokeMantaCoreStopping();
			for (int i = 0; i < _SmtpServers.Count; i++)
				(_SmtpServers[i] as SmtpServer).Dispose();

			Logging.Info("Manta MTA Service has stopped.");
		}
	}
}
