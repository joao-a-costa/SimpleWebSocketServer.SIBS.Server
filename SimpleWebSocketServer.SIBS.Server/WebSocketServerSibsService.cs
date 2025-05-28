using System;
using System.Reflection;
using System.ServiceProcess;

namespace SimpleWebSocketServer.SIBS.Server
{
    public class SocketService : ServiceBase
    {
        private const string _WebSocketServerPrefix = "https://+:10005/";
        private const string _MessageErrorErrorOccurred = "Error occurred";

        private WebSocketServerSibs _server;

        public SocketService()
        {
            ServiceName = Assembly.GetExecutingAssembly().GetName().Name;
        }

        protected override void OnStart(string[] args)
        {
            string prefix = _WebSocketServerPrefix;

            // Create an instance of WebSocketServer
            _server = new WebSocketServerSibs();

            try
            {
                // Start the WebSocket server
                _server.Start(prefix);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_MessageErrorErrorOccurred}: {ex.Message}");
            }
            finally
            {
                // Ensure to stop the server if it's running
                if (_server.IsStarted)
                {
                    _server.Stop();
                }
            }
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}
