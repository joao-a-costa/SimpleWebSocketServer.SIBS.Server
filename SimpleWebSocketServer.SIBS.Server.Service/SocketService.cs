using System;
using System.Reflection;
using System.ServiceProcess;
using System.Collections.Concurrent;

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

        public SocketService()
        {
            ServiceName = Assembly.GetExecutingAssembly().GetName().Name;
        }

        public void Start(string prefix)
        {
            _server = new WebSocketServerSibs();
            _server.Start(prefix);
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
