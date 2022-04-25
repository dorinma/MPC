﻿using MPCProtocol;
using MPCProtocol.Requests;
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
        private ManualResetEvent sendDone;
        private ManualResetEvent connectServerDone;
        private ManualResetEvent serversSend;

        private Socket listener;
        private static Protocol protocol = Protocol.Instance;
        private object usersLock = new object();

        private string instance;

        private int totalUsers;
        private int connectedUsers;
        private string sessionId;

        SortRandomRequest sortRandomRequest = default;
        
        private int operation; // 1.merge 2.find the K'th element 3.sort
        private List<UInt16> values;

        private List<Socket> clientsSockets;
        private SERVER_STATE serverState;

        private Socket memberServerSocket;


        public CommunicationServer(string instance)
        {
            this.instance = instance;

            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<UInt16>();
            sessionId = string.Empty;
            clientsSockets = new List<Socket>();
            serverState = SERVER_STATE.FIRST_INIT;

            acceptDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            connectServerDone = new ManualResetEvent(false);
            serversSend = new ManualResetEvent(false);

        }

        public void RestartServer()
        {
            operation = 0;
            totalUsers = 0;
            connectedUsers = 0;
            values = new List<UInt16>();
            sessionId = string.Empty;
            serverState = SERVER_STATE.FIRST_INIT;
            clientsSockets = new List<Socket>();
            acceptDone.Reset();
            sendDone.Reset();
            connectServerDone.Reset();
            serversSend.Reset();
        }

        public void OpenSocket()
        {
            try
            {
                Console.WriteLine($"[INFO] Server {instance} started.");
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, instance == "A" ? 2022 : 2023));
                listener.Listen(10);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<UInt16> StartServer()
        {
            try
            {
                while (serverState == SERVER_STATE.FIRST_INIT || connectedUsers < totalUsers)
                {
                    acceptDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    acceptDone.WaitOne();
                }
                Console.WriteLine("exit while");
                //endSession.waitOne 
                return values;
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
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Signal the main thread to continue.  
            acceptDone.Set();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;
            
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(RecieveCallback), state);
        }

        public void AcceptCallback2(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Signal the main thread to continue.  
            connectServerDone.Set();

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(RecieveCallback), state);
        }

        public void RecieveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.  
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.  
            if (read > 0)
            {
                if (!protocol.ValidateMessage(state.buffer))
                {
                    SendError(handler, ServerConstants.MSG_VALIDATE_PROTOCOL_FAIL);
                    return;
                }

                protocol.ParseData(state.buffer, out OPCODE_MPC opcode, out Byte[] MsgData);
                Console.WriteLine($"opcode {opcode}");
                if (!ValidateServerState(opcode))
                {
                    SendError(handler, ServerConstants.MSG_VALIDATE_SERVER_STATE_FAIL);
                    return; // todo check
                }

                AnalyzeMessage(opcode, MsgData, handler);

                /*clientsSockets.Add(handler);
                connectedUsers++;*/

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(RecieveCallback), state);
            }
            else
            {
                /*if (state.sb.Length > 1)
                {
                    // All the data has been read from the client;  
                    // display it on the console.  
                    string content = state.sb.ToString();
                    Console.WriteLine($"Read {content.Length} bytes from socket.\n Data : {content}");
                }*/

                // TODO continue listening if there are more numbers to send
                
                //handler.Close();
            }
        }

        private void Send(Socket socket ,byte[] byteData)
        {
            // Begin sending the data to the remote device.  
            socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

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

            byte[] message = protocol.CreateStringMessage(OPCODE_MPC.E_OPCODE_ERROR, $"Error: {errMsg}");
            Send(socket, message);
        }

        private bool ValidateServerState(OPCODE_MPC opcode)
        {
            return ServerConstants.statesMap[opcode] == serverState;
        }

        public void AnalyzeMessage(OPCODE_MPC Opcode, byte[] data, Socket socket)
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
                case OPCODE_MPC.E_OPCODE_ERROR: //todo is this needed? client will send error to server?
                    {
                        //RespondServerDone(Data);
                        break;
                    }
                default:
                    break;
            }
        }

        private void HandleSortRandomness(byte[] data, Socket socket)
        {
            sortRandomRequest = JsonConvert.DeserializeObject<SortRandomRequest>(Encoding.Default.GetString(data)) ?? default;
            if (sortRandomRequest != default) // send confirmation
            {
                Send(socket, protocol.CreateStringMessage(OPCODE_MPC.E_OPCODE_SERVER_VERIFY, sortRandomRequest.sessionId));
            }
            else // Error - wrong format
            {
                SendError(socket, "Bad random sort request");
            }
        }

        private void HandleClientInit(byte[] Data, Socket socket)
        {
            if (string.Empty != Interlocked.CompareExchange(ref sessionId, Randomness.GenerateSessionId(), string.Empty))
            {
                // Session is already in motion
                SendError(socket, ServerConstants.MSG_SESSION_RUNNING);
                return;
            }

            if (!protocol.GetClientInitParams(Data, out int userOperation, out int participants))
            {
                // Failed to parse parameters
                SendError(socket, ServerConstants.MSG_VALIDATE_PARAMS_FAIL);
                return;
            }

            //send session id to second server
            operation = userOperation;
            totalUsers = participants;
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            SendSessionDetailsToServer();
            Console.WriteLine($"session id {sessionId}, participants {totalUsers}, servsr state {serverState}");

            byte[] message = protocol.CreateStringMessage(OPCODE_MPC.E_OPCODE_SERVER_INIT, sessionId);
            Send(socket, message);
        }

        public void HandleServerInit(byte[] data, Socket serverSocket)
        {
            if (protocol.GetServerInitParams(data, out sessionId, out operation, out totalUsers))
            {
                // Failed to parse parameters
                memberServerSocket = serverSocket;
                serverState = SERVER_STATE.CONNECT_AND_DATA;
                Console.WriteLine($"session id {sessionId}, participants {totalUsers}, servsr state {serverState}");
            }
            else
            {
                Console.WriteLine($"Failed to parse server to server int message.");
            }
        }

        private void HandleClientData(byte[] Data, Socket socket)
        {
            Console.WriteLine("habdle client data");
            if (!protocol.GetDataParams(Data, out string session, out UInt32 elementsCounter, out List<UInt16> elements))
            {
                // failed to parse parameters
                SendError(socket, ServerConstants.MSG_VALIDATE_PARAMS_FAIL);
                return;
            }
            if (!session.Equals(sessionId))
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
                serverState = totalUsers == connectedUsers ? SERVER_STATE.DATA : serverState;
                //state.numberOfDataElements += ElementsCounter;
                values.AddRange(elements);
                Console.WriteLine($"values {values.Count}");
                Console.WriteLine($"total users {totalUsers}, connected users {connectedUsers}");
                clientsSockets.Add(socket);
                Console.WriteLine($"Clients sockets count {clientsSockets.Count}");

                acceptDone.Set();
            }
        }

        public void SendOutputMessage(string data)
        {
            byte[] message = protocol.CreateStringMessage(OPCODE_MPC.E_OPCODE_SERVER_MSG, data);
            clientsSockets.ForEach(socket => Send(socket, message));
        }

        public void SendOutputData(List<UInt16> data)
        {
            byte[] message = protocol.CreateArrayMessage(OPCODE_MPC.E_OPCODE_SERVER_DATA, sizeof(UInt16), data.ToArray());
            clientsSockets.ForEach(socket => Send(socket, message));
        }

        public void SendSessionDetailsToServer()
        {
            Console.WriteLine($"Send to other server: operation {operation}, total users {totalUsers}");
            byte[] message = protocol.CreateSessionAndOperationMessage(OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT, sessionId, sizeof(int), new int[] { operation, totalUsers });
            Send(memberServerSocket, message);
            //serversSend.WaitOne();
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
                
                if (instance == "A")
                {
                    // Connect to the remote endpoint.  
                    memberServerSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), memberServerSocket);
                    Console.WriteLine($"ip: {serverIp} port: {serverPort}");
                    connectServerDone.WaitOne();
                }        
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

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectServerDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
