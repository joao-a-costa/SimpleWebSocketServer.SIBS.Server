using System;
using System.ServiceProcess;

namespace SimpleWebSocketServer.SIBS.Server.Service
{
    internal static class Program
    {
        #region "Constants"

        private const string _WebSocketServerPrefix = "https://+:10005/";
        private const string _MessageEnterJSONCommand = "Enter 'q' to stop:";
        private const string _MessageErrorErrorOccurred = "Error occurred";
        private const string _MessagePressAnyKeyToExit = "Press any key to exit...";

        #endregion

        #region "Members"

        private static SocketService server;

        #endregion

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        static void Main()
        {
#if DEBUG

            RunSimulation();

#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SocketService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

        private static void RunSimulation()
        {
            // Define the WebSocket server prefix
            string prefix = _WebSocketServerPrefix;

            // Create an instance of WebSocketServer
            server = new SocketService();

            try
            {
                // Start the WebSocket server
                server.Start(prefix);
                System.Console.WriteLine(_MessageEnterJSONCommand);
                System.Console.WriteLine("Type 'list' to see connected clients.");

                string input;
                while (true)
                {
                    input = System.Console.ReadLine()?.ToLower();

                    if (input == "q")
                        break;

                    if (input == "list")
                    {
                        PrintConnectedClients();
                    }
                    else
                    {
                        System.Console.WriteLine("Invalid command. Type 'list' to see connected clients or 'q' to quit.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"{_MessageErrorErrorOccurred}: {ex.Message}");
            }
            finally
            {
                // Ensure to stop the server if it's running
                if (server.IsStarted)
                {
                    server.Stop();
                }
            }

            System.Console.WriteLine(_MessagePressAnyKeyToExit);
            System.Console.ReadKey();
        }

        #region "Private Methods"

        public static void PrintConnectedClients()
        {
            var terminals = server.Terminals;
            var fronts = server.Fronts;
            var terminalToFrontMap = server.TerminalToFrontMap;

            System.Console.WriteLine("=== Connected Terminals ===");
            if (terminals.Count == 0)
                System.Console.WriteLine("No terminals connected.");
            else
            {
                foreach (var kvp in terminals)
                {
                    System.Console.WriteLine($"Terminal ID: {kvp.Key} | Value: {kvp.Value}");
                }
            }

            System.Console.WriteLine("\n=== Connected Fronts ===");
            if (fronts.Count == 0)
                System.Console.WriteLine("No fronts connected.");
            else
            {
                foreach (var kvp in fronts)
                {
                    System.Console.WriteLine($"Front ID: {kvp.Key} | Value: {kvp.Value}");
                }
            }

            System.Console.WriteLine("\n=== Terminal ↔ Front Mapping ===");
            if (terminalToFrontMap.Count == 0)
                System.Console.WriteLine("No terminal-front mappings.");
            else
            {
                foreach (var kvp in terminalToFrontMap)
                {
                    System.Console.WriteLine($"Terminal ID: {kvp.Key} ⇄ Front ID: {kvp.Value}");
                }
            }

            System.Console.WriteLine();
        }

        #endregion
    }
}
