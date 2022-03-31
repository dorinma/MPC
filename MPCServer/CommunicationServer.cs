using MPCProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPCServer
{

    public enum SERVER_STATE
    {
        FIRST_INIT = 1,
        CONNECT_AND_DATA = 2,
        DATA = 3,
        COMPUTATION = 4
    }

    /*
    BinaryFormatter formatter = new BinaryFormatter();
    clientList = (List<string>) formatter.Deserialize(networkStream);
     */
    public class Communication
    {
        public const string MSG_VALIDATE_PROTOCOL_FAIL = "Could not parse message.";
        public const string MSG_VALIDATE_SERVER_STATE_FAIL = "The server is currently not accepting this kind of messages.";
        public const string MSG_SESSION_RUNNING = "Session already running.";
        public const string MSG_VALIDATE_PARAMS_FAIL = "Could not parse message parameters.";
        public const string MSG_WRONG_SESSION_ID = "Session id is wrong.";


        private Socket serverSocket;
        private Socket clientSocket; // We will only accept one socket.
        private ManualResetEvent acceptDone;
        private ManualResetEvent sendDone;
        private ManualResetEvent receiveDone;

        private byte[] buffer;
        List<UInt16> values;
        private uint numberOfDataElements;
        private uint numberOfUsers;
        private uint numberOfConnectedUsers;
        private string sessionId;
        Protocol protocol = Protocol.Instance;
        SERVER_STATE serverState;
        public static int counter = 0;



        object usersLock = new object();

        private Dictionary<OPCODE_MPC, SERVER_STATE> statesMap = new Dictionary<OPCODE_MPC, SERVER_STATE>
        {
            { OPCODE_MPC.E_OPCODE_CLIENT_INIT, SERVER_STATE.FIRST_INIT },
            { OPCODE_MPC.E_OPCODE_CLIENT_DATA, SERVER_STATE.CONNECT_AND_DATA },
        };

        public Communication()
        {
            values = new List<UInt16>();
            numberOfDataElements = 0;
            numberOfUsers = 0;
            numberOfConnectedUsers = 0;
            sessionId = string.Empty;
            serverState = SERVER_STATE.FIRST_INIT;
        }

        public void RestartServer()
        {
            values = new List<UInt16>();
            numberOfDataElements = 0;
            numberOfUsers = 0;
            numberOfConnectedUsers = 0;
            sessionId = string.Empty;
            serverState = SERVER_STATE.FIRST_INIT;
        }

        public void OpenSocket()
        {
            try
            {
                Console.WriteLine("[INFO] Server started.");
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, 2022));
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<UInt16> StartServer()
        {
            Console.WriteLine("[INFO] Listening...");
            serverSocket.Listen(0);
            // todo steps
            //1- loop infinity for listen until get msg
            //2- get info abut input and update
            //3- listen to clients according step 2
            //4- computing
            //5- finish and send informative success msg
            //6- restart and back to step 1



            try
            {
                while (serverState != SERVER_STATE.DATA || values.Count != numberOfDataElements)
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

                // Listen for client data.
                clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, null);
                /*if (!protocol.ValidateMessage(buffer))
                {
                    SendError(MSG_VALIDATE_PROTOCOL_FAIL);
                    return; // todo check
                }
                //todo special parse per nulltermintor
                protocol.ParseData(buffer, out OPCODE_MPC opcode, out Byte[] MsgData);
                Console.WriteLine($"opcode {opcode}");
                Console.WriteLine("accept callback");
                if (!ValidateServerState(opcode))
                {
                    SendError(MSG_VALIDATE_SERVER_STATE_FAIL);
                    return; // todo check
                }
                AnalyzeMessage(opcode, MsgData);
                // Continue listening for clients.
                serverSocket.BeginAccept(AcceptCallback, null);*/
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

        private bool ValidateServerState(OPCODE_MPC opcode)
        {
            return statesMap[opcode] == serverState;
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
                    SendError(MSG_VALIDATE_PROTOCOL_FAIL);
                    return; // todo check
                }
                protocol.ParseData(buffer, out OPCODE_MPC opcode, out Byte[] MsgData);
                Console.WriteLine($"opcode {opcode}");
                Console.WriteLine("recieve callback");
                if (!ValidateServerState(opcode))
                {
                    SendError(MSG_VALIDATE_SERVER_STATE_FAIL);
                    return; // todo check
                }
                AnalyzeMessage(opcode, MsgData);
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

        public void SendData(OPCODE_MPC opcode, List<UInt16> values)
        {
            try
            {
                if (clientSocket.Connected)
                {
                    byte[] message = protocol.CreateMessage(opcode, sizeof(UInt16), values.ToArray());
                    clientSocket.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallback), null);
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
        }

        public void SendError(string errMsg)
        {
            SendString(OPCODE_MPC.E_OPCODE_ERROR, "Error: " + errMsg, toClient: true);
        }

       /* public void SendStr(string msg, string dest)
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
        }*/

        public void SendString(OPCODE_MPC opcode, string msg, bool toClient)
        {
            var socket = toClient ? clientSocket : serverSocket;
            var buffer = protocol.CreateMessage(opcode, sizeof(char), msg.ToCharArray());
            try
            {
                if (socket.Connected)
                {
                    socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, SendCallback, null);
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
            values.ForEach(number => bytes.AddRange(BitConverter.GetBytes(number)));
            bytes.Add(0xA);
            return bytes.ToArray();
        }

        /*private List<UInt16> GetValues(byte[] Data)
        {
            List<UInt16> output = new List<UInt16>();
            byte nullTerminator = 0xA;
            for (int i = 0; i <= Data.Length - sizeof(UInt16) && Data[i] != nullTerminator; i+=sizeof(UInt16))
            {
                output.Add(BitConverter.ToUInt16(buffer, i));
            }
            return output;
        }*/

        public void AnalyzeMessage(OPCODE_MPC Opcode, byte[] Data)
        {
            counter++;
            Console.WriteLine($"counter {counter}");
            switch (Opcode)
            {
                case OPCODE_MPC.E_OPCODE_CLIENT_INIT:
                    {
                        HandleClientInit(Data);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_CLIENT_DATA:
                    {
                        HandleClientData(Data);
                        break;
                    }
                case OPCODE_MPC.E_OPCODE_ERROR: //todo is this needed? client will send error to server?
                    {
                        //RespondServerDone(Data);
                        break;
                    }
                default:
                    break;
            }
        }

        private void HandleClientInit(byte[] Data)
        {
            if (string.Empty != Interlocked.CompareExchange(ref sessionId, GenerateSessionId(), string.Empty))
            {
                // Session is already in motion
                SendError(MSG_SESSION_RUNNING);
            }

            if (!protocol.GetInitParams(Data, out uint Participants))
            {
                // Failed to parse parameters
                SendError(MSG_VALIDATE_PARAMS_FAIL);
            }

            numberOfUsers = Participants;
            serverState = SERVER_STATE.CONNECT_AND_DATA;
            Console.WriteLine($"session id {sessionId}, participants {numberOfUsers}, servsr state {serverState}");
            SendString(OPCODE_MPC.E_OPCODE_SERVER_INIT, sessionId.ToString(), toClient: true);
        }

        private void HandleClientData(byte[] Data)
        {
            //if(serverState != SERVER_STATE.CONNECT_AND_DATA)
            if (!protocol.GetDataParams(Data, out string Session, out UInt32 ElementsCounter, out List<UInt16> Elements))
            {
                // failed to parse parameters
                SendError(MSG_VALIDATE_PARAMS_FAIL);
                return;
            }
            if (!CompareSessionId(Session.ToCharArray()))
            {
                // wrong session id
                SendError(MSG_WRONG_SESSION_ID);
                return;
            }

            lock (usersLock)
            {
                // check if all users are already connected
                if (numberOfConnectedUsers == numberOfUsers)
                {
                    // error
                    return;
                }
                numberOfConnectedUsers++;
                serverState = numberOfConnectedUsers == numberOfUsers ? SERVER_STATE.DATA : serverState;
                numberOfDataElements += ElementsCounter;
                values.AddRange(Elements);
                Console.WriteLine($"server state {serverState}");
            }
        }

        private bool CompareSessionId(char[] retSession)
        {
            char[] currSession = sessionId.ToCharArray();
            for (int i = 0; i < currSession.Length; i++)
            {
                if (currSession[i] != retSession[i * 2])
                    return false;
            }
            return true;
        }

        private string GenerateSessionId()
        {
            return Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
