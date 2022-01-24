using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    /*
    BinaryFormatter formatter = new BinaryFormatter();
    clientList = (List<string>) formatter.Deserialize(networkStream);
     */
    public class Communication
    {
        private Socket serverSocket;
        private Socket clientSocket; // We will only accept one socket.
        private byte[] buffer;
        List<UInt16> values;
        private int usersCounter; 
        private int dataCounter; 

        public Communication(List<UInt16> valuesList, int users, int data)
        {
            values = valuesList;
            usersCounter = users;
            dataCounter = data;
        }

        public List<UInt16> StartServer()
        {
            try
            {
                Console.WriteLine("[INFO] Server started.");
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 2022));
                serverSocket.Listen(1);
                Console.WriteLine("[INFO] Listening...");
                while (values.Count < usersCounter * dataCounter)
                {
                    serverSocket.BeginAccept(AcceptCallback, null);
                }
                return values;
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                Console.WriteLine("[INFO] A client is trying to connect.");

                clientSocket = serverSocket.EndAccept(AR);
                buffer = new byte[clientSocket.ReceiveBufferSize];

                // Send a message to the newly connected client.
                // var sendData = Encoding.ASCII.GetBytes("[SERVER] Hello Client!");
                // clientSocket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, SendCallback, null);
                // Listen for client data.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                // Continue listening for clients.
                serverSocket.BeginAccept(AcceptCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndSend(AR);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                // Socket exception will raise here when client closes, as this sample does not
                // demonstrate graceful disconnects for the sake of simplicity.
                int received = clientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }
                values.AddRange(GetValues());
                // Start receiving data again.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            // Avoid Pokemon exception handling in cases like these.
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendData(List<UInt16> values, string dest)
        {
            byte[] buffer = IntListToByteArray(values);
            try
            {
                if(dest == "DataClient")
                {
                    if (clientSocket.Connected)
                        clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                }
                else if (dest == "Server")
                {
                    //if (serverSocket.Connected)
                      //  serverSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                }
                else
                    throw new NotImplementedException(); //todo fix
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
        }

        public void SendStr(string msg, string dest)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            try
            {
                if (dest == "DataClient")
                {
                    Console.WriteLine("success");
                    if (clientSocket.Connected)
                    {
                        Console.WriteLine("senddd");
                        clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                        //while (true){ }
                    }
                }
                else if (dest == "Server")
                {
                    //if (serverSocket.Connected)
                    //  serverSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                }
                else
                {
                    Console.WriteLine("faileeee");
                    throw new NotImplementedException(); //todo fix
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
        }
        private static byte[] IntListToByteArray(List<UInt16> values)
        {
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < values.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(values.ElementAt(i)));
            }
            bytes.Add(0xA);
            return bytes.ToArray();
        }

        private List<UInt16> GetValues()
        {
            List<UInt16> output = new List<UInt16>();
            byte nullTerminator = 0xA;
            for (int i = 0; i < buffer.Length - sizeof(UInt16) && buffer[i] != nullTerminator; i+=sizeof(UInt16))
            {
                output.Add(BitConverter.ToUInt16(buffer, i));
            }
            return output;
        }

    }
}
