using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Uhuru.Prison.ChangeSession
{
    [RunInstaller(true)]
    public partial class WindowsServiceInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller process = new ServiceProcessInstaller();
        private ServiceInstaller service = new ServiceInstaller();
        public WindowsServiceInstaller()
        {
            InitializeComponent();

            this.Installers.Add(process);
            this.Installers.Add(service);
        }

        // more info on args here:
        // http://stackoverflow.com/questions/4862580/using-installutil-to-install-a-windows-service-with-startup-parameters
        public override void Install(IDictionary stateSaver)
        {
            process.Account = ServiceAccount.LocalSystem;
            service.ServiceName = "ChangeSession";

            if (this.Context.Parameters["service-id"] != null)
            {
                service.ServiceName += "-" + this.Context.Parameters["service-id"];

                var path = this.Context.Parameters["assemblypath"];
                if (path[0] != '"')
                {
                    path = '"' + path + '"';
                }

                path += " " + this.Context.Parameters["service-id"];
                this.Context.Parameters["assemblypath"] = path;
            }

            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            service.ServiceName = "ChangeSession";

            if (this.Context.Parameters["service-id"] != null)
            {
                service.ServiceName += "-" + this.Context.Parameters["service-id"];
            }

            base.Uninstall(savedState);
        }
    }
}
