using MPCTools;
using MPCTools.Requests;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MPCRandomnessClient
{
    public class CommunicationRandClient
    {
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);

        private static Protocol protocol = Protocol.Instance;

        private Socket socket;
        public string sessionId = string.Empty;
        public bool serversVerified = false;

        public void Reset()
        {
            sessionId = string.Empty;
            serversVerified = false;
            CloseSocket();
        }

        public void Connect(string serverIp, int serverPort)
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

                // Create a TCP/IP socket.  
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), socket);
                Console.WriteLine($"[INFO] Connected to server at {serverIp}:{serverPort}.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-1);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket socket = (Socket)ar.AsyncState;

                // Complete the connection.  
                socket.EndConnect(ar);

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void SendMasksAndKeys(RandomRequest sortRequest)
        {
            string data = JsonConvert.SerializeObject(sortRequest);
            MessageRequest messageRequest = protocol.CreateMessage(OPCODE_MPC.E_OPCODE_RANDOM_SORT, data);
            
            Send(messageRequest);
        }
        private void Send(MessageRequest messageRequest)
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
                Socket socket = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = socket.EndSend(ar);

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
                state.workSocket = socket;

                // Begin receiving the data from the remote device.  
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
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
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.  
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the client.
                    MessageRequest messageRequest = protocol.DeserializeRequest<MessageRequest>(content);
                    if (messageRequest == default)
                    {
                        Console.WriteLine($"[ERROR] Bad json format");
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

        public void AnalyzeMessage(OPCODE_MPC Opcode, string data)
        {
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_SERVER_VERIFY:
                    {
                        var sessionReceived = data.Substring(0, ProtocolConstants.SESSION_ID_SIZE);
                        serversVerified = sessionId.Equals(sessionReceived);
                        if (serversVerified)
                        {
                            Console.WriteLine($"[INFO] Received confirmation of session: {sessionId}");
                        }
                        else
                        {
                            Console.WriteLine($"[ERROR] Wrong session id: {sessionId}");
                        }

                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        Console.WriteLine($"[ERROR] Received error: {data}");
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
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] " + ex.Message);
            }
        }
    }
}
