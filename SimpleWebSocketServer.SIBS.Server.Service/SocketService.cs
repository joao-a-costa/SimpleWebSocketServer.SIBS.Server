﻿using System;
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
        private WebSocketServerSibs _server;
        private int port = Program._WebSocketServerDefaultPort;

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
            if (!int.TryParse(iniFile.Read(Program._iniSection, Program._iniPortValue), out port))
                port = Program._WebSocketServerDefaultPort;

            string instanceNameComplete = string.IsNullOrEmpty(serviceName) ? AssemblyName : serviceName;

            ServiceName = instanceNameComplete;
        }

        public void Start()
        {
            // Define the WebSocket server prefix
            string prefixFinal = Program._WebSocketServerPrefix.Replace($"#PORT#", port.ToString());

            _server = new WebSocketServerSibs();
            _server.Start(prefixFinal);
        }

        protected override void OnStart(string[] args)
        {
            Start();
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}
