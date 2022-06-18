using MPCTools;
using MPCTools.Requests;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPCServer
{
    public class CommunicationServer
    {
        private ManualResetEvent acceptDone;
        private ManualResetEvent receiveDone;
        private ManualResetEvent sendDone;
        private ManualResetEvent connectServerDone;
        private ManualResetEvent serversSend;

        private ILogger logger;
        private Socket listener;
        private static Protocol protocol = Protocol.Instance;
        private object usersLock = new object();

        private byte instance;
        private string serverInstance;

        private int totalUsers;
        private int connectedUsers;
        public string sessionId;

        private const int pendingQueueLength = 10;

        public SortRandomRequest sortRandomRequest = default;
        //Future code
        //pubkic Dictionary<OPERATION, SortRandomRequest> requeset;

        public OPERATION operation; // 1.merge 2.find the K'th element 3.sort
        private List<uint> values;
        private uint[] exchangeData = null;

        private List<Socket> clientsSockets;
        private SERVER_STATE serverState;

        private Socket memberServerSocket;

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

            acceptDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            connectServerDone = new ManualResetEvent(false);
            serversSend = new ManualResetEvent(false);
            receiveDone = new ManualResetEvent(false);
        }

        public void setInstance(byte instance)
        {
            this.instance = instance;
            this.serverInstance = instance == 0 ? "A" : "B";
        }

        public void RestartServer()
        {
            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<uint>();
            sessionId = string.Empty;
            serverState = SERVER_STATE.INIT;
            clientsSockets = new List<Socket>();
            acceptDone.Reset();
            sendDone.Reset();
            connectServerDone.Reset();
            serversSend.Reset();
            receiveDone.Reset();
            exchangeData = null;
            //memberServerSocket.Close();
            if(this.instance == 1) //server b
                ServerBBeginReceive(); //TODO if server b gets the message this isnt needed
        }

        private void ServerBBeginReceive()
        {
            if(instance != 1)
            {
                return;
            }

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
                logger.Error($"Failed to receive message. Error: {ex.Message}");
            }
        }

        public bool ConnectServers(string otherServerIp, int otherServerPort)
        {
            bool result = true;
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(otherServerIp), otherServerPort);

                // Create a TCP/IP socket.
                memberServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                memberServerSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), memberServerSocket);
                connectServerDone.WaitOne();
                logger.Debug($"Connected to server with IP: {otherServerIp} port: {otherServerPort}.");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to connect to other server. Error: {ex.Message}");
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
                Environment.Exit(-1);
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
                listener.Listen(pendingQueueLength);

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
            Socket acceptListener = (Socket)ar.AsyncState; //maybe same listener
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
            String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the
                    // client. Display it on the console.  
                    logger.Debug($"Received {content.Length} bytes from socket.");

                    MessageRequest messageRequest = protocol.DeserializeRequest<MessageRequest>(content);
                    
                    if(messageRequest == default)
                    {
                        SendError(handler, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, messageRequest.GetType()));
                        return;
                    }

                    if (!protocol.ValidateMessage(messageRequest.prefix))
                    {
                        SendError(handler, ServerConstants.MSG_VALIDATE_PROTOCOL_FAIL);
                        return;
                    }

                    if (messageRequest.opcode != OPCODE_MPC.E_OPCODE_ERROR && !ValidateServerState(messageRequest.opcode))
                    {
                        SendError(handler, string.Format(ServerConstants.MSG_VALIDATE_SERVER_STATE_FAIL, serverState, messageRequest.opcode));
                        return; // todo check
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

        private void Send(Socket socket, MessageRequest messageRequest)
        {
            byte[] bytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(messageRequest));
            // Begin sending the data to the remote device.  
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
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
                case OPCODE_MPC.E_OPCODE_ERROR: //todo is this needed? client will send error to server?
                    {
                        logger.Error($"Server Receive error. Error: {data}");
                        //RespondServerDone(Data);
                        break;
                    }       
                default:
                    break;
            }
        }

        private void HandleSortRandomness(string data, Socket socket)
        {
            sortRandomRequest = protocol.DeserializeRequest<SortRandomRequest>(data);
            if (sortRandomRequest != default) // send confirmation
            {
                Send(socket, protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_VERIFY, sortRandomRequest.sessionId));
                //serverState = SERVER_STATE.INIT;
                logger.Debug($"Received randomness request for {sortRandomRequest.n} elements.");
            }
            else // Error - wrong format
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
            }

            //send session id to second server
            operation = clientInitRequest.operation;
            totalUsers = clientInitRequest.numberOfUsers;
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            SendSessionDetailsToServer();

            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_INIT, sessionId);
            Send(socket, messageRequest);
            //wait for starting client data
            StateObject state = new StateObject();
            state.workSocket = socket;
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
            logger.Info($"Start sesion {sessionId}");
            logger.Info($"Operation - {operation}, Number of participants - {totalUsers}");
        }

        public void HandleServerInit(string data, Socket serverSocket)
        {
            ServerToServerInitRequest serverToServerInitRequest = protocol.DeserializeRequest<ServerToServerInitRequest>(data);
            if (serverToServerInitRequest == default)
            {
                logger.Error(string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, serverToServerInitRequest.GetType()));
            }

            sessionId = serverToServerInitRequest.sessionId;
            operation = serverToServerInitRequest.operation;
            totalUsers = serverToServerInitRequest.numberOfUsers;
            memberServerSocket = serverSocket; // server B save server A's socket
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            logger.Info($"Start sesion {sessionId}");
            logger.Info($"Operation - {operation}, Number of participants - {totalUsers}");
        }

        private void HandleClientData(string data, Socket socket)
        {
            DataRequest clientDataRequest = protocol.DeserializeRequest<DataRequest>(data);
            if (clientDataRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, clientDataRequest.GetType()));
            }
            
            if (!clientDataRequest.sessionId.Equals(sessionId))
            {
                // wrong session id
                SendError(socket, ServerConstants.MSG_WRONG_SESSION_ID);
                return;
            }

            lock (usersLock)
            {
                // check if all users are already connected
                if (totalUsers == connectedUsers)
                {
                    SendError(socket, ServerConstants.MSG_ALL_USERS_CONNECTED);
                    return;
                }

                connectedUsers++;
                serverState = totalUsers == connectedUsers ? SERVER_STATE.COMPUTATION : serverState;
                values.AddRange(clientDataRequest.dataElements);
                clientsSockets.Add(socket);

                acceptDone.Set(); //TODO - should we check that connectedUsers == totalUsers
                logger.Debug($"User connected and added {clientDataRequest.dataElements.Length} elements. Number of connected users - {connectedUsers}");
            }
        }

        public void SendMessageToAllClients(OPCODE_MPC opcode ,string message)
        {
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_MSG, message);
            clientsSockets.ForEach(socket => Send(socket, messageRequest));
        }

        public void SendOutputData(uint[] outputShares)
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
                numberOfUsers = totalUsers
            };

            string data = JsonConvert.SerializeObject(serverToServerInitRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT, data);

            Send(memberServerSocket ,messageRequest);
        }

        public void HandleServerExchangeData(string data, Socket socket)
        {
            DataRequest dataRequest = protocol.DeserializeRequest<DataRequest>(data);
            if (dataRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, dataRequest.GetType()));
            }
            else
            {
                logger.Debug($"Server {serverInstance} receive {dataRequest.dataElements.Length} elements from the other server.");
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
            logger.Debug($"Server {serverInstance} send the other server his diff values");
        }

        internal uint[] ReceiveServerData()
        {
            if (exchangeData == null)
            {
                receiveDone.Reset();
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
                    logger.Error($"Failed to receive message. Error: {ex.Message}");
                }
                receiveDone.WaitOne();
            }

            uint[] currExchangeData = exchangeData;
            exchangeData = null;

            return currExchangeData;
        }

        internal uint[] BreceiveServerData()
        {
            if (exchangeData == null)
            {
                receiveDone.Reset();
                receiveDone.WaitOne();
            }

            uint[] currExchangeData = exchangeData;
            exchangeData = null;

            return currExchangeData;
        }

    }

}