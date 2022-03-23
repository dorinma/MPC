using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

// State object for receiving data from remote device.  
public class StateObject
{
    // Client socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  
    public const int BufferSize = 256;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];
    // Received data string.  
    public StringBuilder sb = new StringBuilder();
}

public class CommunicationDataClient<T>
{
    // The port and ip of the remote device.  
    private int port;
    private string ip;
    private Socket client;
    // ManualResetEvent instances signal completion.  
    private ManualResetEvent connectDone;
    private ManualResetEvent sendDone;
    private ManualResetEvent receiveDone;
    MPCProtocol.Protocol protocol = MPCProtocol.Protocol.Instance;


    // The response from the remote device.  
    public String response { get; set; }
    public List<UInt16> dataResponse { get; set; }


    public CommunicationDataClient(string ip, int port)
    {
        this.ip = ip; 
        this.port = port;
        response = string.Empty;
        dataResponse = new List<UInt16>();
        connectDone = new ManualResetEvent(false);
        sendDone = new ManualResetEvent(false);
        receiveDone = new ManualResetEvent(false);
    }

    public void Connect()
    {
        // Connect to a remote device.  
        try
        {
            // Establish the remote endpoint for the socket.  
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ip), port);

            // Create a TCP/IP socket.  
            client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            
            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
            Console.WriteLine($"ip: {ip} port: {port}");
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

            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public void SendRequest(List<T> data)
    {
        // Send data to the remote device.
        byte[] byteData = new byte[data.Count * SizeOf(typeof(T)) + 1 + protocol.GetHeaderSize()];
        Buffer.BlockCopy(protocol.CreateHeaderDataMsg(), 0, byteData, 0, byteData.Length - 1); //header
        Buffer.BlockCopy(data.ToArray(), 0, byteData, 4, byteData.Length - 1); //data
        byteData[byteData.Length - 1] = 0XA;
        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
        sendDone.WaitOne();
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

    private static int SizeOf(Type type)
    {
        switch (type)
        {
            case Type UInt16:
                return sizeof(UInt16);
            default:
                return sizeof(UInt16);
        }
    }

    public void ReceiveRequest()
    {
        try
        {
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = client;

            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
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
            // Retrieve the state object and the client socket
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.  
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // Get the rest of the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                var message = state.sb.ToString();
                if (message.StartsWith("Message:"))
                {
                    response = message;
                }
                else
                {
                    dataResponse.AddRange(GetValues(state.buffer));
                }
                receiveDone.Set();
            }
            /*else
            {
                Console.WriteLine("errr");
                // All the data has arrived; put it in response.  
                if (state.sb.Length > 1)
                {
                    Console.WriteLine("errr2");
                    var message = state.sb.ToString();
                    if (message.StartsWith("C"))
                    {
                        Console.WriteLine(message);
                        response = message;
                    }
                }
                // Signal that all bytes have been received.  
                receiveDone.Set();
            }*/
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private List<UInt16> GetValues(byte[] buffer)
    {
        List<UInt16> output = new List<UInt16>();
        byte nullTerminator = 0xA;
        for (int i = 0; i < buffer.Length - sizeof(UInt16) && buffer[i] != nullTerminator; i += sizeof(UInt16))
        {
            output.Add(BitConverter.ToUInt16(buffer, i));
        }
        return output;
    }

    public void WaitForReceive()
    {
        receiveDone.WaitOne();
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

    



/*private static void Send(Socket client, String data)
{
    // Convert the string data to byte data using ASCII encoding.  
    byte[] byteData = Encoding.ASCII.GetBytes(data);

    // Begin sending the data to the remote device.  
    client.BeginSend(byteData, 0, byteData.Length, 0,
        new AsyncCallback(SendCallback), client);
}*/









    

    
}


/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace MPCDataClient
{
    public class CommunicationDataClient<T>
    {
        private Socket clientSocket;
        private int port = 2021;
        private byte[] buffer;
        byte[] feedback;

        public CommunicationDataClient()
        {
            this.port = 2021;
            this.feedback = new byte[1024];
        }


        //Sync operations
        public void SyncConnect(String serverIP)
        {
            try
            {
                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // Connect to the specified host.
                var endPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
                this.clientSocket.Connect(endPoint);
                Console.WriteLine("Socket connected to {0}", clientSocket.RemoteEndPoint.ToString());
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}. Exiting..", ex.Message);
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception : {0}. Exiting..", ex.ToString());
                Environment.Exit(-1);
            }
        }

        public void SyncSendRequest(List<T> values)
        {
            try
            {
                byte[] buffer = new byte[values.Count * SizeOf(typeof(T))];
                Buffer.BlockCopy(values.ToArray(), 0, buffer, 0, buffer.Length);
                this.clientSocket.Send(buffer);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}. Exiting..", ex.Message);
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception : {0}. Exiting..", ex.ToString());
                Environment.Exit(-1);
            }
        }

        public String SyncReceiveRequest()
        {
            try
            {
                int count = clientSocket.Receive(feedback);
                return Encoding.ASCII.GetString(feedback, 0, count);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}. Exiting..", ex.Message);
                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected exception : {0}. Exiting..", ex.ToString());
                Environment.Exit(-1);
            }
            return null;
        }

        //Async operations
        public void AsyncConnect(String serverIP)
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

        public void AsyncSendRequest(List<UInt16> values)
        {
            buffer = new byte[values.Count * SizeOf(typeof(T))];
            Buffer.BlockCopy(values.ToArray(), 0, buffer, 0, buffer.Length);
            try
            {
                if (clientSocket.Connected) { 
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                    }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            *//*
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                if(clientSocket.Connected)
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }*//*
        }

        *//*private static byte[] IntListToByteArray(List<UInt16> values)
        {
            List<byte> bytes = new List<byte>();
            for(int i = 0; i < values.Count; i++)
            {
                bytes.AddRange(BitConverter.GetBytes(values.ElementAt(i)));
            }
            bytes.Add(0xA);
            return bytes.ToArray();
        }*//*

        private void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndConnect(AR);
                buffer = new byte[clientSocket.ReceiveBufferSize];
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

        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                int received = clientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }

                string message = Encoding.ASCII.GetString(buffer);

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
       
        private void SendCallback(IAsyncResult AR)
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

        private static int SizeOf(Type type)
        {
            switch (type)
            {
                case Type UInt16:
                    return sizeof(UInt16);
                default:
                    return sizeof(UInt16);
            }
        }

    }
}
*/