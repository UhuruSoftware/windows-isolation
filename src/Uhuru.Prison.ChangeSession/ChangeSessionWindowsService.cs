using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Uhuru.Prison.ExecutorService;

namespace Uhuru.Prison.ChangeSession
{
    partial class ChangeSessionWindowsService : ServiceBase
    {
        private ServiceHost serviceHost = null;
        private string serviceId;

        public ChangeSessionWindowsService()
        {
            InitializeComponent();
        }

        public ChangeSessionWindowsService(string pServiceId)
            : this()
        {
            this.serviceId = pServiceId;
        }


        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            if (this.serviceHost != null)
            {
                this.serviceHost.Close();
            }

            // start ServiceHost 
            var baseUri = new Uri(Prison.changeSessionBaseEndpointAddress);
            Uri serviceUri = baseUri;
            //Debugger.Launch();
            if (this.serviceId != null)
            {
                serviceUri = new Uri(Prison.changeSessionBaseEndpointAddress + "/" + this.serviceId);
            }

            var bind = new NetNamedPipeBinding();
            bind.Security.Mode = NetNamedPipeSecurityMode.Transport;
            bind.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            serviceHost = new ServiceHost(typeof(Executor), serviceUri);

            //serviceHost.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = false, HttpsGetEnabled = false });
            //serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            //serviceHost.Description.Behaviors.Find<ServiceDebugBehavior>().HttpHelpPageUrl = serviceUri;

            serviceHost.AddServiceEndpoint(typeof(IExecutor), bind, string.Empty);

            //this.serviceHost = new ServiceHost(typeof(Executor));

            this.serviceHost.Open();
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (this.serviceHost != null)
            {
                this.serviceHost.Close();
                this.serviceHost = null;
            }
        }

        public static void Main(string[] args)
        {
            string serviceId = null;
            if (args.Length > 0)
            {
                serviceId = args[0];
            }

            ServiceBase.Run(new ChangeSessionWindowsService(serviceId));
        }
    }
}
