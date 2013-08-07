using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MantaService
{
	[RunInstaller(true)]
	public class MantaServiceInstaller : Installer
	{
		/// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
		public MantaServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = 
                               new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
			serviceInstaller.ServiceName = "MantaMTA";
            serviceInstaller.DisplayName = "Manta MTA";
            serviceInstaller.StartType = ServiceStartMode.Manual;
            

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
	}
}
