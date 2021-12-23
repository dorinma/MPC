using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace MPCDataClient
{
    public class Communication
    {
        private static Socket clientSocket;
        private static byte[] buffer;
        private static int port = 2021;

        public static void Connect(String serverIP)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connect to the specified host.
                var endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
                clientSocket.BeginConnect(endPoint, ConnectCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException : {0}", ex.Message);
            }
        }

        public static void SendRequest(string serverIP, string message)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
        }

        private static void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndConnect(AR);
                buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                Console.WriteLine("Message received at 2: {0}", Encoding.ASCII.GetString(buffer)); //TODO delete
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException : {0}", ex.Message);
            }
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                int received = clientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }

                string message = Encoding.ASCII.GetString(buffer);
                Console.WriteLine("Message received at 1: {0}", message); //TODO delete
                /*Invoke((Action)delegate
                {
                    Text = "Server says: " + message;
                });*/

                // Start receiving data again.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException : {0}", ex.Message);
            }
        }
       
        private static void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndSend(AR);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException : {0}", ex.Message);
            }
        }

    }
}
