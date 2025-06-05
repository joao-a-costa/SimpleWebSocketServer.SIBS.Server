using System;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.ServiceProcess;
using System.Collections.Concurrent;
using SimpleWebSocketServer.SIBS.Server.Service.Controllers;

namespace SimpleWebSocketServer.SIBS.Server.Service
{
    public class SocketService : ServiceBase
    {
        private const string _WebSocketServerPrefix = "https://+:10005/";

        private WebSocketServerSibs _server;

        public bool IsStarted => _server.IsStarted;
        public ConcurrentDictionary<Guid, long> Terminals => _server.Terminals;
        public ConcurrentDictionary<Guid, Guid> Fronts => _server.Fronts;
        public ConcurrentDictionary<Guid, Guid> TerminalToFrontMap => _server.TerminalToFrontMap;

        public string AssemblyName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name;

        public SocketService()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\" + AssemblyName + ".exe"
            );

            var iniPath = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(config.FilePath));
            var iniFile = new IniFileController($"{Path.GetDirectoryName(config.FilePath)}\\{iniPath}");

            string serviceName = iniFile.Read(Program._iniSection, Program._iniServiceNameValue);

            string instanceNameComplete = string.IsNullOrEmpty(serviceName) ? AssemblyName : serviceName;

            ServiceName = instanceNameComplete;
        }

        public void Start(string prefix)
        {
            var iniFile = new IniFileController(Program._iniFile);
            var port = int.TryParse(iniFile.Read(Program._iniSection, Program._iniPortValue), out int parsedPort) ? parsedPort : Program._WebSocketServerDefaultPort;

            // Define the WebSocket server prefix
            string prefixFinal = _WebSocketServerPrefix.Replace($"#PORT#", port.ToString());

            _server = new WebSocketServerSibs();
            _server.Start(prefixFinal);
        }

        protected override void OnStart(string[] args)
        {
            Start(_WebSocketServerPrefix);
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}
