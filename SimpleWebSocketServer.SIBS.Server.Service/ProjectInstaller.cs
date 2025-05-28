using System.Reflection;
using System.ComponentModel;
using System.Configuration.Install;

namespace SimpleWebSocketServer.SIBS.Server.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public string AssemblyName { get; set; } = $"SmartCASLESS - Server";

        public ProjectInstaller()
        {
            InitializeComponent();
        }

        public ProjectInstaller(string assemblyName)
        {
            InitializeComponent();

            AssemblyName = assemblyName;
        }

        private void SetupServiceName()
        {
            string instanceNameComplete = AssemblyName;

            serviceInstaller1.ServiceName = instanceNameComplete;
            serviceInstaller1.DisplayName = instanceNameComplete;
            serviceInstaller1.Description = instanceNameComplete;
        }

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            SetupServiceName();
        }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            SetupServiceName();
        }
    }
}