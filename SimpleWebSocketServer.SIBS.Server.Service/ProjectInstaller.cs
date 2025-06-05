using System.Reflection;
using System.ComponentModel;
using System.Configuration.Install;
using SimpleWebSocketServer.SIBS.Server.Service.Controllers;
using System.IO;
using System.Configuration;

namespace SimpleWebSocketServer.SIBS.Server.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public string AssemblyName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name;

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
            Configuration config = ConfigurationManager.OpenExeConfiguration(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + AssemblyName + ".exe"
            );

            var iniPath = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(config.FilePath));
            var iniFile = new IniFileController($"{Path.GetDirectoryName(config.FilePath)}\\{iniPath}");

            string serviceName = iniFile.Read(Program._iniSection, Program._iniServiceNameValue);

            string instanceNameComplete = string.IsNullOrEmpty(serviceName) ? AssemblyName : serviceName;

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