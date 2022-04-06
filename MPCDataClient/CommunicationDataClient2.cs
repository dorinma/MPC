using MPCProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPCDataClient
{
    public class CommunicationDataClient2
    {
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);

        private Socket client;
        private static Protocol protocol = Protocol.Instance;

        public string sessionId { get; set; }
        public string response = string.Empty;
        public List<UInt16> dataResponse = new List<UInt16>();

        public void Connect(string serverIp, int serverPort)
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);

                // Create a TCP/IP socket.  
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                Console.WriteLine($"ip: {serverIp} port: {serverPort}");
                connectDone.WaitOne();
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
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Send(byte[] byteData)
        {
            // Convert the string data to byte data using ASCII encoding.  
            //byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendCallback), client);
        }

        public string SendInitMessage(int operation, int numberOfUsers)
        {
            byte[] message = protocol.CreateArrayMessage(OPCODE_MPC.E_OPCODE_CLIENT_INIT, sizeof(int), new List<int> { operation, numberOfUsers }.ToArray());
            Send(message);
            //client.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallback), client);
            //sendDone.WaitOne();
            Receive();
            receiveDone.WaitOne();
            return sessionId;
        }

        public void SendData(string sessionId, List<UInt16> data)
        {
            Console.WriteLine("Send data to server");
            Console.WriteLine($"data {data.Count}");
            byte[] meesage = protocol.CreateSessionAndDataMessage(OPCODE_MPC.E_OPCODE_CLIENT_DATA, sessionId, sizeof(UInt16), data.ToArray());
            Send(meesage);
        }


        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
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
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
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
                    receiveDone.Set();
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

        public void AnalyzeMessage(OPCODE_MPC Opcode, byte[] Data)
        {
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_SERVER_INIT:
                    {
                        sessionId = Encoding.Default.GetString(Data).Substring(0, ProtocolConstants.SESSION_ID_SIZE);
                        Console.WriteLine($"recieved session id - {sessionId}");
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_SERVER_MSG:
                    {
                        response = Encoding.Default.GetString(Data);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_SERVER_DATA:
                    {
                        dataResponse = MPCConvertor.BytesToList(Data, 0);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        Console.WriteLine($"Recived error: {Encoding.Default.GetString(Data)}");
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
