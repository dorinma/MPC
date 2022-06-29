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

namespace MPCDataClient
{
    public class CommunicationDataClient
    {
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);

        private Socket client;
        private static Protocol protocol = Protocol.Instance;

        public string sessionId { get; set; }
        public string response = string.Empty;
        public List<uint> dataResponse = new List<uint>();

        public bool Connect(string serverIp, int serverPort)
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

                // Create a TCP/IP socket.  
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                var ar = client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
                Socket socket = (Socket)ar.AsyncState;

                if (!socket.Connected)
                {
                    Console.WriteLine("Could not connect to the servers.");
                    return false;
                }

                Console.WriteLine($"Connected to server with IP: {serverIp}, port: {serverPort}.");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return false;
                //Environment.Exit(-1);
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

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                connectDone.Set();
            }
        }

        private void Send(MessageRequest messageRequest)
        {
            byte[] bytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(messageRequest));
            // Begin sending the data to the remote device.  
            client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
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
                //sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Receive()
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void ReceiveCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the client. Display it on the console.  
                    Console.WriteLine($"Read {content.Length} bytes from");

                    MessageRequest messageRequest = protocol.DeserializeRequest<MessageRequest>(content);
                    if (messageRequest == default)
                    {
                        Console.WriteLine($"Error: Invalid json format.");
                        return;
                    }
                    
                    if (!protocol.ValidateMessage(messageRequest.prefix))
                    {
                        Console.WriteLine("Error: Invalid header.");
                        return;
                    }

                    AnalyzeMessage(messageRequest.opcode, messageRequest.data);
                    receiveDone.Set();
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                }
            }
        }

        public string SendInitMessage(OPERATION operation, int numberOfUsers, bool debugMode)
        {
            ClientInitRequest clientInitRequest = new ClientInitRequest()
            {
                operation = operation,
                numberOfUsers = numberOfUsers,
                debugMode = debugMode
            };

            string data = JsonConvert.SerializeObject(clientInitRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_CLIENT_INIT, data);

            Send(messageRequest);
        
            Receive();
            receiveDone.WaitOne();
            return sessionId;
        }
        
        public void SendSharesToServer(string sessionId, uint[] dataShares)
        {
            DataRequest clientDataRequest = new DataRequest()
            {
                sessionId = sessionId,
                dataElements = dataShares
            };

            string data = JsonConvert.SerializeObject(clientDataRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_CLIENT_DATA, data);
            //byte[] meesage = protocol.CreateSessionAndDataMessage(OPCODE_MPC.E_OPCODE_CLIENT_DATA, sessionId, sizeof(uint), data);
            Send(messageRequest);
        }


        public void AnalyzeMessage(OPCODE_MPC Opcode, string data)
        {
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_SERVER_INIT:
                    {
                        sessionId = data.Substring(0, ProtocolConstants.SESSION_ID_SIZE); //TODO data and randomness clients
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_SERVER_MSG:
                    {
                        response = data;
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_SERVER_DATA:
                    {
                        DataRequest dataRequest = protocol.DeserializeRequest<DataRequest>(data);
                        if (dataRequest != default)
                        {
                            dataResponse = dataRequest.dataElements.ToList();
                            response = "The computation was successful.";
                        }
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        response = data;
                        //HandleError(data);
                        break;
                    }
                default:
                    break;
            }
        }

        public void CloseSocket()
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
