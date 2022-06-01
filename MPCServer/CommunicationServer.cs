using MPCTools;
using MPCTools.Requests;
using Newtonsoft.Json;
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
        private ManualResetEvent reciveDone;
        private ManualResetEvent sendDone;
        private ManualResetEvent connectServerDone;
        private ManualResetEvent serversSend;

        private Socket listener;
        private static Protocol protocol = Protocol.Instance;
        private object usersLock = new object();

        private byte instance;

        private int totalUsers;
        private int connectedUsers;
        private string sessionId;

        public SortRandomRequest sortRandomRequest = default;
        //Future code
        //pubkic Dictionary<OPERATION, SortRandomRequest> requeset;

        private int operation; // 1.merge 2.find the K'th element 3.sort
        private List<uint> values;
        private uint[] exchangeData = null;

        private List<Socket> clientsSockets;
        private SERVER_STATE serverState;

        private Socket memberServerSocket;

        public CommunicationServer()
        {
            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<uint>();
            sessionId = string.Empty;
            clientsSockets = new List<Socket>();
            serverState = SERVER_STATE.OFFLINE;

            acceptDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            connectServerDone = new ManualResetEvent(false);
            serversSend = new ManualResetEvent(false);
            reciveDone = new ManualResetEvent(false);
        }

        public void setInstance(byte instance)
        {
            this.instance = instance;
        }

        public void RestartServer()
        {
            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<uint>();
            sessionId = string.Empty;
            serverState = SERVER_STATE.FIRST_INIT;
            clientsSockets = new List<Socket>();
            acceptDone.Reset();
            sendDone.Reset();
            connectServerDone.Reset();
            serversSend.Reset();
            reciveDone.Reset();
            exchangeData = null;
        }

        public void OpenSocket(int port)
        {
            try
            {
                Console.WriteLine($"Server {instance} started.");
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(10);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public uint[] StartServer()
        {
            try
            {
                while (serverState == SERVER_STATE.OFFLINE || serverState == SERVER_STATE.FIRST_INIT || connectedUsers < totalUsers)
                {
                    acceptDone.Reset();

                   // Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    acceptDone.WaitOne();
                }
                //Console.WriteLine("exit while");
                return values.ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);

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
                        SendError(handler, ServerConstants.MSG_VALIDATE_SERVER_STATE_FAIL);
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
       
        private void Send(Socket socket ,byte[] byteData) //TODO remove
        {
            // Begin sending the data to the remote device.  
            socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendError(Socket socket, string errMsg)
        {

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
                        Console.WriteLine("Server Receive error");
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
                serverState = SERVER_STATE.FIRST_INIT;
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

            /*if (!protocol.GetClientInitParams(Data, out int userOperation, out int participants))
            {
                // Failed to parse parameters
                SendError(socket, ServerConstants.MSG_VALIDATE_PARAMS_FAIL);
                return;
            }*/

            //send session id to second server
            operation = clientInitRequest.operation;
            totalUsers = clientInitRequest.numberOfUsers;
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            SendSessionDetailsToServer();
            Console.WriteLine($"Session with session id: {sessionId} and number of participants: {totalUsers} started, GOOD LUCK :)");

            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_SERVER_INIT, sessionId);
            Send(socket, messageRequest);
            //wait for starting client data
            StateObject state = new StateObject();
            state.workSocket = socket;
            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
        }

        public void HandleServerInit(string data, Socket serverSocket)
        {
            ServerToServerInitRequest serverToServerInitRequest = protocol.DeserializeRequest<ServerToServerInitRequest>(data);
            if (serverToServerInitRequest == default)
            {
                Console.WriteLine(string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, serverToServerInitRequest.GetType()));
            }

            sessionId = serverToServerInitRequest.sessionId;
            operation = serverToServerInitRequest.operation;
            totalUsers = serverToServerInitRequest.numberOfUsers;
            memberServerSocket = serverSocket; // server B save server A's socket
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            Console.WriteLine($"Session with session id: {sessionId} and number of participants: {totalUsers} started, GOOD LUCK :)");
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
               // Console.WriteLine($"values {values.Count}");
               // Console.WriteLine($"total users {totalUsers}, connected users {connectedUsers}");
                clientsSockets.Add(socket);
                //Console.WriteLine($"Clients sockets count {clientsSockets.Count}");

                acceptDone.Set(); //TODO - should we check that connectedUsers == totalUsers
            }
        }

        public void SendOutputMessage(string message)
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

        public void ConnectServers(string serverIp, int serverPort)
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

                // Create a TCP/IP socket.  
                memberServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                memberServerSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), memberServerSocket);
                connectServerDone.WaitOne();
                Console.WriteLine($"Connected to server with ip: {serverIp} port: {serverPort}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Environment.Exit(-1);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

               // Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectServerDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void HandleServerExchangeData(string data, Socket socket)
        {
            DataRequest dataRequest = protocol.DeserializeRequest<DataRequest>(data);
            if (dataRequest == default)
            {
                SendError(socket, string.Format(ServerConstants.MSG_BAD_MESSAGE_FORMAT, dataRequest.GetType()));
            }

            exchangeData = dataRequest.dataElements;
            reciveDone.Set();
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
            Console.WriteLine($"Server {instance} send to the other server his diff values");
        }

        internal uint[] ReciveServerData()
        {
            if (exchangeData == null)
            {
                reciveDone.Reset();
                try
                {
                    // Create the state object.  
                    StateObject state = new StateObject();
                    state.workSocket = memberServerSocket;

                    // Begin receiving the data from the remote device.  
                    memberServerSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                reciveDone.WaitOne();
            }

            uint[] currExchangeData = exchangeData;
            exchangeData = null;

            //Console.WriteLine("recive done :)");
            return currExchangeData;
        }

        internal uint[] BReciveServerData()
        {
            if (exchangeData == null)
            {
                reciveDone.Reset();
                reciveDone.WaitOne();
            }

            uint[] currExchangeData = exchangeData;
            exchangeData = null;

            //Console.WriteLine("recive done :)");
            return currExchangeData;
        }

    }

}