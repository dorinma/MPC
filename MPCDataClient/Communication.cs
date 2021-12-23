using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace MPCDataClient
{
    public class Communication <T>
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

        public static void SendRequest(List<UInt16> values)
        {
            byte[] buffer = IntListToByteArray(values);
            try
            {
                if (clientSocket.Connected)
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            /*
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                if(clientSocket.Connected)
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }*/
        }

        private static byte[] IntListToByteArray(List<UInt16> values)
        {
            List<byte> bytes = new List<byte>();
            for(int i = 0; i < values.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(values.ElementAt(i)));
            }
            bytes.Add(0xA);
            return bytes.ToArray();
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
