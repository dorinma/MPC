using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{

    public enum SERVER_STATE
    {
        FIRST_INIT = 1,
        INIT_AND_DATA = 2,
        DATA = 3
    } 


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
        private uint dataCounter;
        private uint usersCounter; 
        private uint numOfUsersConected;  
        MPCProtocol.Protocol protocol = MPCProtocol.Protocol.Instance;
        SERVER_STATE serverSstate;

        public Communication(List<UInt16> valuesList)//, int users, int data)
        {
            values = valuesList;
            dataCounter = 0;
            usersCounter = 0;
            numOfUsersConected = 0;
            serverSstate = SERVER_STATE.FIRST_INIT;
        }

        public void RestartServer()
        {
            dataCounter = 0;
            usersCounter = 0;
            numOfUsersConected = 0;
            serverSstate = SERVER_STATE.FIRST_INIT;
        }

        public List<UInt16> StartServer()
        {

            // todo steps
            //1- loop infinity for listen until get msg
            //2- get info abut input and update
            //3- listen to clients according step 2
            //4- computing
            //5- finish and send informative success msg
            //6- restart and back to step 1

            RestartServer();

            try
            {
                Console.WriteLine("[INFO] Server started.");
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 2022));
                serverSocket.Listen(0);
                Console.WriteLine("[INFO] Listening...");

                while (!(serverSstate == SERVER_STATE.DATA) || !(values.Count == dataCounter))
                {
                    serverSocket.BeginAccept(AcceptCallback, null);
                }

                /*                serverSocket.Listen(1);
                                Console.WriteLine("[INFO] Listening...");
                                while (values.Count < usersCounter * dataCounter)
                                {
                                    serverSocket.BeginAccept(AcceptCallback, null);
                                } 
                */
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
                if (!protocol.ValidateMessage(buffer))
                {
                    //todo send error
                    return; // todo check
                }
                //todo special parse per nulltermintor
                protocol.ParseData(buffer, out UInt16 Opcode, out Byte[] MsgData);
                AnalyzeMessage(Opcode, MsgData);
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
                //values.AddRange(GetValues());
                if (!protocol.ValidateMessage(buffer))
                {
                    //todo send error
                    return; // todo check
                }
                protocol.ParseData(buffer, out UInt16 Opcode, out Byte[] MsgData);
                AnalyzeMessage(Opcode, MsgData);
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
                    throw new NotImplementedException(); //TODO fix
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
                    if (clientSocket.Connected)
                    {
                        clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                    }
                }
                else if (dest == "Server")
                {
                    //if (serverSocket.Connected)
                    //  serverSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
                }
                else
                {
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

        private List<UInt16> GetValues(byte[] Data)
        {
            List<UInt16> output = new List<UInt16>();
            byte nullTerminator = 0xA;
            for (int i = 0; i <= Data.Length - sizeof(UInt16) && Data[i] != nullTerminator; i+=sizeof(UInt16))
            {
                output.Add(BitConverter.ToUInt16(buffer, i));
            }
            return output;
        }

        public void AnalyzeMessage(UInt16 Opcode, byte[] Data)
        {
            switch (Opcode)
            {
                case (UInt16)MPCProtocol.OPCODE_MPC.E_OPCODE_SERVER_DONE:
                    {
                        protocol.GetServerDone(Data, out byte Status);
                        RespondServerDone(Status);
                        break;
                    }
                case (UInt16)MPCProtocol.OPCODE_MPC.E_OPCODE_INIT:
                    {
                        protocol.GetInitParams(Data, out uint Participants, out uint InputsCount);
                        RespondReceiveInit(Participants, InputsCount);
                        break;
                    }
                case (UInt16)MPCProtocol.OPCODE_MPC.E_OPCODE_DATA:
                    {
                        RespondReceiveData(Data);
                        break;
                    }
                case (UInt16)MPCProtocol.OPCODE_MPC.E_OPCODE_ERROR:
                    {
                        //RespondServerDone(Data);
                        break;
                    }
                default:
                    break;
            }
        }

        private void RespondServerDone(byte Status)
        {

        }

        private void RespondReceiveData(byte[] Data)
        {
            values.AddRange(GetValues(Data));
        }
        //where does the server send the response?

        private void RespondReceiveInit(uint Participants, uint InputsCount)
        {
            if (usersCounter == 0)
            {
                usersCounter = Participants;
                numOfUsersConected++;
                serverSstate = SERVER_STATE.INIT_AND_DATA;
            }
            else if (usersCounter != Participants)
            {
                //TODO send error message to the clients
            }

            if (numOfUsersConected == usersCounter)
            {
                serverSstate = SERVER_STATE.DATA;
            }

            dataCounter += InputsCount;
        }
    }
}
