using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using SimpleWebSocketServer.SIBS.Server.Helper;
using SimpleWebSocketServer.SIBS.Server.Models;

namespace SimpleWebSocketServer.SIBS.Server
{
    public class WebSocketServerSibs
    {
        private const string _MessageReceivedUnknownMessage = "Received unknown message";
        private const string _MessageErrorOccurred = "Error occurred";
        private const string _MessageErrorDeserializingMessage = "Error deserializing message";
        private const string _MessageErrorNotConnectedToATerminal = "Not connected to a terminal";
        private const string _MessageStartingServer = "Starting server";

        #region "Fields"

        /// <summary>
        /// The WebSocket server
        /// </summary>
        private WebSocketServer server;
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region "Events"

        public delegate void ClientConnectedEventHandler(object sender, Guid clientId, string message);
        public delegate void ClientDisconnectedEventHandler(object sender, EventArgs e);
        public delegate void TerminalStatusReqResponseReceivedEventHandler(object sender, TerminalStatusReqResponse reqResponse);
        public delegate void InstallCertificateMessageEventHandler(object sender, string e);

        public event ClientConnectedEventHandler ClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;
        public event TerminalStatusReqResponseReceivedEventHandler TerminalStatusReqResponseReceived;
        public static event InstallCertificateMessageEventHandler InstallCertificateMessage;

        #endregion

        #region "Properties"

        public bool IsStarted => server.IsStarted;

        private readonly ConcurrentDictionary<Guid, int> _terminals = new ConcurrentDictionary<Guid, int>();
        private readonly ConcurrentDictionary<Guid, Guid> _fronts = new ConcurrentDictionary<Guid, Guid>();
        private readonly ConcurrentDictionary<Guid, Guid> _terminalToFrontMap = new ConcurrentDictionary<Guid, Guid>();

        public ConcurrentDictionary<Guid, int> Terminals => _terminals;
        public ConcurrentDictionary<Guid, Guid> Fronts => _fronts;
        public ConcurrentDictionary<Guid, Guid> TerminalToFrontMap => _terminalToFrontMap;

        #endregion

        #region "Public methods"

        /// <summary>
        /// Start the WebSocket server
        /// </summary>
        /// <param name="prefix">The prefix to listen to</param>
        public void Start(string prefix)
        {
            // Create an instance of WebSocketServer
            server = new WebSocketServer(_logger, prefix);

            // Initialize the cancellation token source
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // This is expected when the task is canceled
            Log($"{_MessageStartingServer}: {prefix}");

            // Start the WebSocket server asynchronously
            serverTask = Task.Run(() =>
            {
                try
                {
                    // Define events for server lifecycle
                    server.ServerStarted += Server_ServerStarted;
                    server.ClientConnected += Server_ClientConnected;
                    server.ClientDisconnected += Server_ClientDisconnected;
                    server.MessageReceived += Server_MessageReceived;

                    // Start the WebSocket server
                    server.Start();

                    // Keep the server running until a cancellation is requested
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // You can add more logic here if needed (e.g., monitor the server)
                        Task.Delay(1000, cancellationToken).Wait(cancellationToken); // Poll every 1 second
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // This is expected when the task is canceled
                    Log($"{_MessageErrorOccurred}: {ex.Message}");
                }
                catch (HttpListenerException ex)
                {
                    // Log and handle HttpListener specific exceptions
                    Log($"{_MessageErrorOccurred}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Log any other exceptions
                    Log($"{_MessageErrorOccurred}: {ex.Message}");
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        /// <returns>The task</returns>
        public void Stop()
        {
            // Signal the task to cancel
            cancellationTokenSource?.Cancel();

            // Stop the WebSocket server
            if (serverTask != null && !serverTask.IsCompleted)
            {
                server.Stop().Wait();
                //serverTask.Wait();  // Optionally wait for the task to complete
            }

            // Dispose of the cancellation token source
            cancellationTokenSource?.Dispose();
        }

        public async Task SendMessageToClients(List<(Guid clientId, string message)> messages)
        {
            var tasks = messages.Select(m => server.SendMessageToClient(m.clientId, m.message));
            await Task.WhenAll(tasks);
        }


        /// <summary>
        /// Send a message to the terminal
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The task</returns>
        public async Task SendMessageToTerminal((Guid clientId, string message) e)
        {
            var terminal = _terminalToFrontMap.FirstOrDefault(kvp => kvp.Value == e.clientId);

            if (!terminal.Equals(default(KeyValuePair<Guid, Guid>)))
            {
                await server.SendMessageToClient(terminal.Key, e.message);
            }
            else
            {
                await SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(new ErrorNotification{Message = _MessageErrorNotConnectedToATerminal}))});
            }
        }

        /// <summary>
        /// Send a message to the front
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The task</returns>
        public async Task SendMessageToFront((Guid clientId, string message) e)
        {
            // Broadcast message to front
            if (_terminalToFrontMap.ContainsKey(e.clientId))
            {
                var frontId = _terminalToFrontMap[e.clientId];
                await server.SendMessageToClient(frontId, e.message);
            }
        }

        //public static async Task<bool> InstallCertificate(string prefix, string certificatePath, string certificatePassword,
        //    string appId, string certificateThumbprint)
        //{
        //    var res = false;

        //    try
        //    {
        //        WebSocketServer.InstallCertificateMessage += WebSocketServer_InstallCertificateMessage;
        //        res = await WebSocketServer.InstallCertificate(prefix, certificatePath, certificatePassword, appId, certificateThumbprint);
        //    }
        //    catch(Exception ex)
        //    {
        //        Log($"{_MessageErrorOccurred}: {ex.Message}");
        //    }
        //    finally
        //    {
        //        WebSocketServer.InstallCertificateMessage -= WebSocketServer_InstallCertificateMessage;
        //    }
        //    return res;
        //}

        public static Tuple<bool, string> AddFirewallRule(string applicationName, string prefix)
        {
            var res = false;
            var resMessage = string.Empty;

            try
            {
                res = FirewallHelper.AddInboundRule(applicationName, (ushort)FirewallHelper.GetIpAndPort(prefix).port);
            }
            catch (Exception ex)
            {
                resMessage = $"{_MessageErrorOccurred}: {ex.Message}";
            }

            return Tuple.Create(res, resMessage);
        }

        #endregion

        #region "Private methods"

        /// <summary>
        /// Method to log messages to the console
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            _logger.Info(message);
        }

        /// <summary>
        /// OnClientConnected event handler
        /// </summary>
        private void OnClientConnected(Guid clientId, string message)
        {
            ClientConnected?.Invoke(this, clientId, message);
        }

        /// <summary>
        /// OnClientDisconnected event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private void Server_ClientDisconnected(object sender, (Guid clientId, string message) e)
        {
            // Unregister terminal if not already
            if (_terminals.ContainsKey(e.clientId))
                _terminals.TryRemove(e.clientId, out _);

            // Unregister front if not already
            if (_fronts.ContainsKey(e.clientId))
                _fronts.TryRemove(e.clientId, out _);

            // Unlink terminal from front
            var terminal = _terminalToFrontMap.FirstOrDefault(kvp => kvp.Key == e.clientId);
            if (!terminal.Equals(default(KeyValuePair<Guid, Guid>)))
            {
                _terminalToFrontMap.TryRemove(terminal.Key, out _);
                SendMessageToClients(new List<(Guid clientId, string message)> { (terminal.Value, JsonConvert.SerializeObject(new TerminalDisconnected())) }).Wait();
            }

            // Unlink front from terminal
            var front = _terminalToFrontMap.FirstOrDefault(kvp => kvp.Value == e.clientId);
            if (!front.Equals(default(KeyValuePair<Guid, Guid>)))
                _terminalToFrontMap.TryRemove(front.Key, out _);

            ClientDisconnected?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// OnMessageReceived event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private void Server_MessageReceived(object sender, (Guid clientId, string message) e)
        {
            try
            {
                //// Convert JSON string to dynamic object
                //dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(e.message);

                //if (jsonObj != null && jsonObj.Property("isFront") != null)
                //    ProcessFrontMessage(e);

                ProcessMessage(e);
            }
            catch (Exception ex)
            {
                // Handle deserialization errors
                Log($"{_MessageErrorDeserializingMessage}: {ex.Message}");
            }
        }

        //private void ProcessFrontMessage((Guid clientId, string message) e)
        //{
        //    // Try to parse the message as a TerminalStatusReqResponse object
        //    var resultFront = JsonConvert.DeserializeObject<TerminalStatusReqResponse>(e.message);

        //    if (resultFront != null)
        //    {


        //    }
        //}

        /// <summary>
        /// Process the received message
        /// </summary>
        /// <param name="e">The message</param>
        private void ProcessMessage((Guid clientId, string message) e)
        {
            try
            {
                // Try to parse the message as a TerminalStatusReqResponse object
                var resultTerminal = JsonConvert.DeserializeObject<TerminalStatusReqResponse>(e.message);

                if (resultTerminal != null)
                {
                    switch (resultTerminal.Type)
                    {
                        case Enums.Enums.RequestType.STATUS_RESPONSE:
                            OnTerminalStatusReqResponseReceived(resultTerminal);
                            break;
                        case Enums.Enums.RequestType.SET_AUTH_CREDENTIAL_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.TX_REQUEST:
                            SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(new TransactionResponse()))}).Wait();
                            break;
                        case Enums.Enums.RequestType.REGISTER_FRONT_REQUEST:
                            RegisterFront(e).Wait();
                            break;
                        case Enums.Enums.RequestType.LIST_TERMINALS_REQUEST:
                            ListTerminals(e).Wait();
                            break;
                        case Enums.Enums.RequestType.LINQ_TERMINAL_TO_FRONT_REQUEST:
                            LinkTerminalToFront(e).Wait();
                            break;
                        case Enums.Enums.RequestType.EVENT_NOTIFICATION:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.PROCESS_PAYMENT_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.PROCESS_PAYMENT_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.ERROR_NOTIFICATION:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.HEARTBEAT_NOTIFICATION:
                            // Register terminal if not already
                            if (resultTerminal.TerminalId > 0 && !_terminals.ContainsKey(e.clientId))
                            {
                                Log($"TerminalID: {resultTerminal.TerminalId} associated to Client: {e.clientId}");
                                _terminals[e.clientId] = resultTerminal.TerminalId;

                                // Broadcast message informing there's a new terminal
                                var rewTerminalConnectedReq = new NewTerminalConnectedReq
                                {
                                    TerminalId = resultTerminal.TerminalId,
                                    ClientId = e.clientId
                                };

                                string messageJson = JsonConvert.SerializeObject(rewTerminalConnectedReq);

                                // Create a list of tasks to send messages asynchronously to all clients
                                var sendTasks = _fronts.Keys.Select(clientId =>
                                    SendMessageToClients(new List<(Guid clientId, string message)>
                                    {
                                        (clientId, messageJson)
                                    })
                                );

                                // Await all tasks in parallel
                                Task.WhenAll(sendTasks).Wait();

                            }
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.RECEIPT_NOTIFICATION:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.PAIRING_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.PAIRING_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.PAIRING_NOTIFICATION:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.RECONCILIATION_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.RECONCILIATION_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.REFUND_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.REFUND_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.COMMUNICATIONS_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.COMMUNICATIONS_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.GET_MERCHANT_DATA_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.GET_MERCHANT_DATA_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.SET_MERCHANT_DATA_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.SET_MERCHANT_DATA_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.CONFIG_TERMINAL_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.CONFIG_TERMINAL_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.CUSTOMER_DATA_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.CUSTOMER_DATA_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.LOYALTY_INQUIRY_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.LOYALTY_INQUIRY_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.PENDING_REVERSALS_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.PENDING_REVERSALS_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        case Enums.Enums.RequestType.DELETE_PENDING_REVERSALS_REQUEST:
                            SendMessageToTerminal(e).Wait();
                            break;
                        case Enums.Enums.RequestType.DELETE_PENDING_REVERSALS_RESPONSE:
                            SendMessageToFront((e.clientId, e.message)).Wait();
                            break;
                        default:
                            SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(new ErrorNotification{Message = _MessageReceivedUnknownMessage}))}).Wait();
                            Log(_MessageReceivedUnknownMessage);
                            break;
                    }
                }
                else
                {
                    SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(new ErrorNotification { Message = _MessageReceivedUnknownMessage }))}).Wait();
                    Log(_MessageReceivedUnknownMessage);
                }
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Log($"{_MessageErrorOccurred}: {ex.Message}");
                SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(new ErrorNotification{Message =  $"{_MessageErrorOccurred}: {ex.Message}"}))}).Wait();
            }
        }

        #region "Front operations"

        private async Task RegisterFront((Guid clientId, string message) e)
        {
            var messageToSend = string.Empty;
            var response = new RegisterFrontResponse();
            var registerFrontReq = JsonConvert.DeserializeObject<RegisterFrontReq>(e.message);

            // Register front if not already
            if (!_fronts.ContainsKey(e.clientId))
            {
                _fronts[e.clientId] = registerFrontReq.Front;
                messageToSend = $"Front {registerFrontReq.Front} registered";
            }
            else
                messageToSend = $"Front {registerFrontReq.Front} registered";

            response.Front = e.clientId;
            response.Message = messageToSend;

            await SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(response))});
        }

        private async Task ListTerminals((Guid clientId, string message) e)
        {
            var response = new ListTerminalsResponse();

            response.Terminals.AddRange(_terminals.Values);

            await SendMessageToClients(new List<(Guid clientId, string message)> { (e.clientId, JsonConvert.SerializeObject(response)) });
        }

        private async Task LinkTerminalToFront((Guid clientId, string message) e)
        {
            var response = new LinqTerminalToFrontResponse();

            try
            {
                var linqTerminalToFrontReq = JsonConvert.DeserializeObject<LinqTerminalToFrontReq>(e.message);

                response.Front = linqTerminalToFrontReq.Front;
                response.Terminal = linqTerminalToFrontReq.Terminal;

                var match = _terminals.FirstOrDefault(kvp => kvp.Value == linqTerminalToFrontReq.Terminal);
                if (match.Equals(default(KeyValuePair<Guid, int>)))
                {
                    response.Message = $"Terminal {linqTerminalToFrontReq.Terminal} not connected";
                }
                else if (_terminalToFrontMap.ContainsKey(match.Key))
                {
                    response.Message = $"Terminal {linqTerminalToFrontReq.Terminal} already linked to front {_terminalToFrontMap[match.Key]}";
                }
                else
                {
                    // All good — perform link
                    _terminalToFrontMap[match.Key] = e.clientId;
                    response.Message = $"Linked terminal {linqTerminalToFrontReq.Terminal} to front {linqTerminalToFrontReq.Front}";
                    response.Success = true;
                }
            }
            catch (JsonException ex)
            {
                response.Message = $"Invalid request format: {ex.Message}";
            }

            Log(response.Message);

            await SendMessageToClients(new List<(Guid clientId, string message)>{(e.clientId, JsonConvert.SerializeObject(response))});
        }

        #endregion

        /// <summary>
        /// OnClientConnected event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private void Server_ClientConnected(object sender, (Guid clientId, string message) e)
        {
            OnClientConnected(e.clientId, e.message);

            var response = new ClientConnectedResponse
            {
                ClientId = e.clientId,
                Message = e.message
            };

            SendMessageToClients(new List<(Guid clientId, string message)> { (e.clientId, JsonConvert.SerializeObject(response)) }).Wait();
        }

        /// <summary>
        /// OnServerStarted event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private void Server_ServerStarted(object sender, string e)
        {
            // To be implemented
        }

        /// <summary>
        /// OnTerminalStatusReqResponseReceived event handler
        /// </summary>
        /// <param name="reqResponse">The response</param>
        private void OnTerminalStatusReqResponseReceived(TerminalStatusReqResponse reqResponse)
        {
            TerminalStatusReqResponseReceived?.Invoke(this, reqResponse);
        }

        /// <summary>
        /// WebSocketServer_InstallCertificateMessage event handler
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The message</param>
        private static void WebSocketServer_InstallCertificateMessage(object sender, string e)
        {
            InstallCertificateMessage?.Invoke(sender, e);
        }

        #endregion
    }
}