using MPCTools;
using MPCTools.Requests;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MPCServer
{
    public class CommunicationServer
    {
        //events
        private ManualResetEvent acceptDone;
        private ManualResetEvent receiveDone;
        private ManualResetEvent sendDone;
        private ManualResetEvent connectServerDone;
        private ManualResetEvent serversSend;

        private static Protocol protocol = Protocol.Instance;
        private object usersLock = new object();
        private ILogger logger;

        private Socket listener;
        private Socket memberServerSocket;
        private List<Socket> clientsSockets;

        private byte instance;
        private string serverInstance;

        private SERVER_STATE serverState;

        private int totalUsers;
        private int connectedUsers;
        public string sessionId;
        public DateTime sessionStartTime;
        public bool debugMode;

        public OPERATION operation; // 1.merge 2.find the K'th element 3.sort
        private List<uint> values;
        public SortRandomRequest sortRandomRequest = default;
        private uint[] exchangeData = null;
        //Future code
        //pubkic Dictionary<OPERATION, SortRandomRequest> requeset;




        public CommunicationServer(ILogger logger)
        {
            this.logger = logger;

            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<uint>();
            sessionId = string.Empty;
            clientsSockets = new List<Socket>();
            serverState = SERVER_STATE.INIT;
            debugMode = true;
            acceptDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            connectServerDone = new ManualResetEvent(false);
            serversSend = new ManualResetEvent(false);
            receiveDone = new ManualResetEvent(false);
        }

        public void setInstance(byte instance)
        {
            serverInstance = instance == 0 ? "A" : "B";
            this.instance = instance;
        }

        public void SetDebugMode(bool debugMode)
        {
            this.debugMode = debugMode;

            if (debugMode)
            {
                if(LogManager.Configuration.FindRuleByName(ServerConstants.debugRuleName) == null)
                {
                    LogManager.Configuration.AddRuleForAllLevels("logconsole", loggerNamePattern: "*");
                    LogManager.Configuration.LoggingRules.Last().RuleName = ServerConstants.debugRuleName;
                }
            }
            else
            {
                LogManager.Configuration.RemoveRuleByName(ServerConstants.debugRuleName);
            }

            LogManager.ReconfigExistingLoggers();
        }

        private bool SessionTimedOut()
        {
            return serverState == SERVER_STATE.CONNECT_AND_DATA &&
                sessionStartTime != default &&
                DateTime.UtcNow - sessionStartTime > TimeSpan.FromMinutes(ServerConstants.SESSION_TIME);
        }


        public void RestartServer()
        {
            logger.Debug($"Restarting server {serverInstance}.");
            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<uint>();
            sessionId = string.Empty;
            sessionStartTime = default;
            serverState = SERVER_STATE.INIT;
            SetDebugMode(true);
            clientsSockets = new List<Socket>();
            exchangeData = null;

            acceptDone.Reset();
            sendDone.Reset();
            connectServerDone.Reset();
            serversSend.Reset();
            receiveDone.Reset();

            if(instance == 1)
            {
                BeginReceiveServer();
            }
        }

        public void ConnectServers(string otherServerIp, int otherServerPort)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (instance == 0)
            {
                bool connected = TryConnect(otherServerIp, otherServerPort);
                while (!connected && stopwatch.Elapsed <= TimeSpan.FromMinutes(ServerConstants.RETRY_TIME))
                {
                    connected = TryConnect(otherServerIp, otherServerPort);
                }

                stopwatch.Stop();

                if (!connected)
                {
                    logger.Error("Could not connect to other server.");
                    Environment.Exit(-1);
                }
            }
        }

        public bool TryConnect(string otherServerIp, int otherServerPort)
        {
            bool result = true;
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(otherServerIp), otherServerPort);

                // Create a TCP-IP socket.
                memberServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                var ar = memberServerSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), memberServerSocket);
                
                connectServerDone.WaitOne();

                Socket socket = (Socket)ar.AsyncState;

                if (socket.Connected)
                {
                    logger.Debug($"Connected to server with IP: {otherServerIp} port: {otherServerPort}.");
                }
                else
                {
                    result = false;
                }

                connectServerDone.Reset();
            }
            catch (Exception ex)
            {
                logger.Error($"Other server if offline, connection failed . Error: {ex.Message}");
                result = false;
            }

            return result;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                logger.Debug($"Socket is connected to {client.RemoteEndPoint.ToString()}.");

                // Signal that the connection has been made.  
                connectServerDone.Set();
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to connect to other server. Error: {ex.Message}");
                connectServerDone.Set();
            }
        }

        public bool OpenSocket(int port)
        {
            bool result = true;
            try
            {
                logger.Debug($"Server {serverInstance} started. Runs on port {port}.");
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(ProtocolConstants.pendingQueueLength);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to open socket. Error: {ex.Message}");
                result = false;
            }
            return result;
        }

        public uint[] StartServer()
        {
            try
            {
                while (serverState == SERVER_STATE.INIT || connectedUsers < totalUsers)
                {
                    acceptDone.Reset();

                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    acceptDone.WaitOne();
                }

                return values.ToArray();
            }
            catch (Exception ex)
            {
                logger.Error($"Accept connection failed. Error: {ex.Message}");
                return null;
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket acceptListener = (Socket)ar.AsyncState;
            Socket handler = acceptListener.EndAccept(ar);

            // Signal the main thread to continue.  
            acceptDone.Set();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                string content = string.Empty;

                // Retrieve the state object and the handler socket from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read more data.  
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the client. Display it on the console.  
                        logger.Debug($"Received {content.Length} bytes from socket.");

                        MessageRequest messageRequest = protocol.DeserializeRequest<MessageRequest>(content);

                        if (messageRequest == default)
                        {
                            SendError(handler, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, messageRequest.GetType()));
                            return;
                        }

                        if (!protocol.ValidateMessage(messageRequest.prefix))
                        {
                            SendError(handler, ServerConstants.MSG_VALIDATE_PROTOCOL_FAIL);
                            return;
                        }

                        if (messageRequest.opcode != OPCODE_MPC.E_OPCODE_ERROR &&
                            messageRequest.opcode != OPCODE_MPC.E_OPCODE_RESTART_SERVER &&
                            !ValidateServerState(messageRequest.opcode))
                        {
                            SendError(handler, string.Format(ServerConstants.MSG_VALIDATE_SERVER_STATE_FAIL, serverState, messageRequest.opcode));

                            if (SessionTimedOut())
                            {
                                logger.Info($"Restarting servers due to timeout.");
                                Send(memberServerSocket, protocol.CreateMessage(OPCODE_MPC.E_OPCODE_RESTART_SERVER, sessionId));

                                RestartServer();
                            }
                            return;
                        }

                        AnalyzeMessage(messageRequest.opcode, messageRequest.data, handler);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error($"Communication error: {e.Message}");
            }            
        }

        private void Send(Socket socket, MessageRequest messageRequest)
        {
            try
            {
                byte[] bytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(messageRequest));
                // Begin sending the data to the remote device.  
                socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            }
            catch(Exception e)
            {
                logger.Error($"Failed to send message. Error: {e.Message}");
                if(socket == memberServerSocket)
                {
                    logger.Error($"Existing...");
                    Environment.Exit(0);
                }
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to send. Error: {ex.Message}");
            }
        }

        public void SendError(Socket socket, string errMsg)
        {
            logger.Error($"Error: {errMsg}");
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_ERROR, $"Error: {errMsg}");
            Send(socket, messageRequest);
        }

        private bool ValidateServerState(OPCODE_MPC opcode)
        {
            return ServerConstants.statesMap[opcode] == serverState;
        }

        public void AnalyzeMessage(OPCODE_MPC Opcode, string data, Socket socket)
        {
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_RANDOM_SORT:
                    {
                        HandleSortRandomness(data, socket);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_CLIENT_INIT:
                    {
                        HandleClientInit(data, socket);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_CLIENT_DATA:
                    {
                        HandleClientData(data, socket);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT:
                    {
                        HandleServerInit(data, socket);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_EXCHANGE_DATA:
                    {
                        HandleServerExchangeData(data, socket);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_RESTART_SERVER:
                    {
                        logger.Error($"Session timed out. Restarting..");
                        RestartServer();
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        logger.Error($"Server received error: {data}");
                        break;
                    }
                default:
                    break;
            }
        }

        private void HandleSortRandomness(string data, Socket socket)
        {
            sortRandomRequest = protocol.DeserializeRequest<SortRandomRequest>(data);
            if (sortRandomRequest != default)
            {
                Send(socket, protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_VERIFY, sortRandomRequest.sessionId)); // Send confirmation
                logger.Debug($"Received randomness request for {sortRandomRequest.n} elements.");
            }
            else
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, sortRandomRequest.GetType()));
            }
        }

        private void HandleClientInit(string data, Socket socket)
        {
            if (string.Empty != Interlocked.CompareExchange(ref sessionId, RandomUtils.GenerateSessionId(), string.Empty))
            {
                // Session is already in motion
                SendError(socket, ServerConstants.MSG_SESSION_RUNNING);
                return;
            }

            ClientInitRequest clientInitRequest = protocol.DeserializeRequest<ClientInitRequest>(data);
            if (clientInitRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, clientInitRequest.GetType()));
                return;
            }

            operation = clientInitRequest.operation;
            totalUsers = clientInitRequest.numberOfUsers;
            sessionStartTime = DateTime.UtcNow;
            SetDebugMode(clientInitRequest.debugMode);

            SendSessionDetailsToServer();
            // Send session id to the data client
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_INIT, sessionId);
            Send(socket, messageRequest);
            // Wait for client data
            StateObject state = new StateObject();
            state.workSocket = socket;
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

            serverState = SERVER_STATE.CONNECT_AND_DATA;

            logger.Info($"Start sesion {sessionId}");
            logger.Info($"Operation - {operation}, Number of participants - {totalUsers}");
        }

        public void HandleServerInit(string data, Socket serverSocket)
        {
            ServerToServerInitRequest serverToServerInitRequest = protocol.DeserializeRequest<ServerToServerInitRequest>(data);
            if (serverToServerInitRequest == default)
            {
                logger.Error(string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, serverToServerInitRequest.GetType()));
                return;
            }

            sessionId = serverToServerInitRequest.sessionId;
            operation = serverToServerInitRequest.operation;
            totalUsers = serverToServerInitRequest.numberOfUsers;
            sessionStartTime = serverToServerInitRequest.sessionStartTime;

            SetDebugMode(serverToServerInitRequest.debugMode);

            memberServerSocket = serverSocket; // Server B saves server A's socket
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            logger.Info($"Start sesion {sessionId}");
            logger.Info($"Operation - {operation}, Number of participants - {totalUsers}");

            BeginReceiveServer(); //Server B listen to server A for case of timeout.
        }

        private void HandleClientData(string data, Socket socket)
        {
            DataRequest clientDataRequest = protocol.DeserializeRequest<DataRequest>(data);
            if (clientDataRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, clientDataRequest.GetType()));
                return;
            }

            if (!clientDataRequest.sessionId.Equals(sessionId))
            {
                // Wrong session id
                SendError(socket, ServerConstants.MSG_WRONG_SESSION_ID);
                return;
            }

            lock (usersLock)
            {
                // Check if all users are already connected
                if (totalUsers == connectedUsers)
                {
                    SendError(socket, ServerConstants.MSG_ALL_USERS_CONNECTED);
                    return;
                }

                connectedUsers++;
                serverState = totalUsers == connectedUsers ? SERVER_STATE.COMPUTATION : serverState;
                values.AddRange(clientDataRequest.dataElements);
                clientsSockets.Add(socket);
                logger.Debug($"New user connected, added {clientDataRequest.dataElements.Length} elements. Number of connected users - {connectedUsers}.");
                
                if(serverState == SERVER_STATE.COMPUTATION)
                {
                    acceptDone.Set(); // wake up the main thread
                }
            }
        }

        public void SendMessageToAllClients(OPCODE_MPC opcode, string message)
        {
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_MSG, message);
            clientsSockets.ForEach(socket => Send(socket, messageRequest));
        }

        public void SendOutputToAllClients(uint[] outputShares)
        {
            DataRequest dataRequest = new DataRequest()
            {
                sessionId = sessionId,
                dataElements = outputShares
            };

            string data = JsonConvert.SerializeObject(dataRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_DATA, data);
            clientsSockets.ForEach(socket => Send(socket, messageRequest));
        }

        public void SendSessionDetailsToServer()
        {
            ServerToServerInitRequest serverToServerInitRequest = new ServerToServerInitRequest()
            {
                sessionId = sessionId,
                operation = operation,
                numberOfUsers = totalUsers,
                debugMode = debugMode,
                sessionStartTime = sessionStartTime
            };

            string data = JsonConvert.SerializeObject(serverToServerInitRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT, data);
            //TODO
            Send(memberServerSocket, messageRequest);
        }

        public void HandleServerExchangeData(string data, Socket socket)
        {
            DataRequest dataRequest = protocol.DeserializeRequest<DataRequest>(data);
            if (dataRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, dataRequest.GetType()));
                return;
            }
            else
            {
                logger.Debug($"Server {serverInstance} received {dataRequest.dataElements.Length} elements from the other server.");
            }

            exchangeData = dataRequest.dataElements;
            receiveDone.Set();
        }

        internal void SendServerData(uint[] diffValues)
        {
            DataRequest dataRequest = new DataRequest()
            {
                sessionId = sessionId,
                dataElements = diffValues
            };

            string data = JsonConvert.SerializeObject(dataRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_EXCHANGE_DATA, data);
            Send(memberServerSocket, messageRequest);
            logger.Debug($"Server {serverInstance} sent the other server his diff values.");
        }

        public void BeginReceiveServer()
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = memberServerSocket;

                // Begin receiving the data from the remote device.  
                memberServerSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to receive message from other server. Error: {ex.Message}\nExisting..");
                Environment.Exit(0);
            }
        }

        public uint[] ReceiveServerData()
        {
            if (exchangeData == null)
            {
                receiveDone.Reset();
                BeginReceiveServer();
                receiveDone.WaitOne();
            }
            
            uint[] currExchangeData = exchangeData;
            exchangeData = null;

            return currExchangeData;

        }
    }
}