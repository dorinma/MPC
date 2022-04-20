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

        private Socket socket;
        private static Protocol protocol = Protocol.Instance;

        public void SendCircuits(LogicCircuit.Circuit c1, LogicCircuit.Circuit c2) { }

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
    }
}
