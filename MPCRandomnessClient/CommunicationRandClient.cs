using MPCProtocol;
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

namespace MPCRandomnessClient
{
    public class CommunicationRandClient
    {
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);

        private static protocol protocol = protocol.Instance;

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
                Console.WriteLine($"ip: {serverIp} port: {serverPort}");
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
                Socket socket = (Socket)ar.AsyncState;

                // Complete the connection.  
                socket.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", socket.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendMasksAndKeys(int n, ulong[] dcfMasks, ulong[] dcfKeys, ulong[] dpfMasks, ulong[] dpfKeys)
        {
            SortRandomRequest sortRequest = new SortRandomRequest
            {
                sessionId = sessionId,
                n = n,
                dcfMasks = dcfMasks,
                dcfKeys = dcfKeys,
                dpfMasks = dpfMasks,
                dpfKeys = dpfKeys
            };

            string message = JsonConvert.SerializeObject(sortRequest);
            Send(protocol.CreateStringMessage(OPCODE_MPC.E_OPCODE_RANDOM_SORT, message));
        }
        private void Send(byte[] byteData)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket socket = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = socket.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                //sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
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
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("recieve callback client");
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    if (!protocol.ValidateMessage(state.buffer))
                    {
                        Console.WriteLine("Error: bad header");
                        return;
                    }

                    protocol.ParseData(state.buffer, out OPCODE_MPC Opcode, out Byte[] MsgData);
                    Console.WriteLine($"opcode {Opcode}");
                    AnalyzeMessage(Opcode, MsgData);
                    receiveDone.Set(); //Ask Hodaya
                    //  Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    /*if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }*/
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void AnalyzeMessage(OPCODE_MPC Opcode, byte[] data)
        {
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_SERVER_VERIFY:
                    {
                        var sessionReceived = Encoding.Default.GetString(data).Substring(0, ProtocolConstants.SESSION_ID_SIZE);
                        serversVerified = sessionId.Equals(sessionReceived);
                        if (serversVerified)
                        {
                            Console.WriteLine($"Received confirmation, session id: {sessionId}");
                        }
                        else
                        {
                            Console.WriteLine($"Wrong session id session id: {sessionId}");
                        }

                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        Console.WriteLine($"Received error: {Encoding.Default.GetString(data)}");
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
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
