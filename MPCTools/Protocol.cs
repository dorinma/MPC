using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using MPCTools.Requests;

namespace MPCTools
{
    public class ProtocolConstants
    {
        public const int HEADER_SIZE = 4;
        public const int SESSION_ID_SIZE = 8;

        public const byte SYNC_LSB = 0; //M
        public const byte SYNC_MSB = 1; //C
        public const byte OPCODE_LSB = 2;

        public const byte NULL_TERMINATOR = 0xA;
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 2048;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    public enum OPCODE_MPC : UInt16
    {
        E_OPCODE_ERROR          = 0xFF,
        
        E_OPCODE_CLIENT_INIT    = 0x01,
        E_OPCODE_SERVER_INIT    = 0x02,
        E_OPCODE_CLIENT_DATA    = 0x03,
        E_OPCODE_SERVER_MSG     = 0x04,  
        E_OPCODE_SERVER_DATA     = 0x05, 
        E_OPCODE_SERVER_TO_SERVER_INIT     = 0x06,
        E_OPCODE_RANDOM_SORT = 0x07,
        E_OPCODE_SERVER_VERIFY = 0x08,
        E_OPCODE_EXCHANGE_DATA = 0x09,
    }

    public enum OPERATION : UInt16
    {
        E_OPER_SORT = 0X01
    }

    public class protocol
    {
        private static protocol instance = null;

        protected AsyncOperation operation;

        public delegate void ServerDone(object sender, byte Status);
        public delegate void Init(object sender, byte Participants, byte InputsCount);

        public event ServerDone Event_ServerDone;
        public event Init Event_Init;

        private protocol()
        {
        }

        public static protocol Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new protocol();
                }
                return instance;
            }
        }

        static void Main(string[] args) 
        {
        }

        public MessageRequest CreateMessage(OPCODE_MPC opcode, string data)
        {
            return new MessageRequest()
            {
                prefix = "MC",
                opcode = opcode,
                data = data,
                suffix = "<EOF>"
            };
        }

        public void ParseData(byte[] Data, out OPCODE_MPC Opcode, out byte[] MsgData)
        {
            Opcode = (OPCODE_MPC)BitConverter.ToUInt16(Data, ProtocolConstants.OPCODE_LSB);
            MsgData = new byte[Data.Length - ProtocolConstants.HEADER_SIZE];
            for (int i = 0; i < MsgData.Length; i++)
                MsgData[i] = Data[i + ProtocolConstants.HEADER_SIZE];
        }



        public bool ValidateMessage(string prefix)
        {
            return prefix.Equals("MC");
        }

        public byte[] CreateArrayMessage(OPCODE_MPC opcode, int elementSize, Array data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] messageBytes = new byte[header.Length + elementSize * data.Length + 1];
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(data, 0, messageBytes, header.Length, data.Length * elementSize); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }

        public byte[] CreateSessionAndOperationMessage(OPCODE_MPC opcode, string sessionId, int elementSize, Array data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] sessionBytes = Encoding.Default.GetBytes(sessionId);
            byte[] messageBytes = new byte[header.Length + sessionBytes.Length + elementSize * data.Length + 1];
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(sessionBytes, 0, messageBytes, header.Length, sessionBytes.Length); //session id
            Buffer.BlockCopy(data, 0, messageBytes, header.Length + sessionBytes.Length, data.Length * elementSize); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }

        public byte[] CreateSessionAndDataMessage(OPCODE_MPC opcode, string sessionId, int elementSize, Array data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] sessionBytes = Encoding.Default.GetBytes(sessionId);
            byte[] messageBytes = new byte[header .Length + sessionBytes.Length + sizeof(int) + elementSize * data.Length + 1];
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(sessionBytes, 0, messageBytes, header.Length, sessionBytes.Length); //elements count
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, messageBytes, header.Length+sessionBytes.Length, sizeof(int)); //session id
            Buffer.BlockCopy(data, 0, messageBytes, header.Length + sessionBytes.Length + sizeof(int), data.Length * elementSize); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }

        public byte[] CreateStringMessage(OPCODE_MPC opcode, string data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] byteData = Encoding.Default.GetBytes(data);
            byte[] messageBytes = new byte[header.Length + byteData.Length + 1];
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(byteData, 0, messageBytes, header.Length, byteData.Length); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }


        public byte[] CreateMessage(OPCODE_MPC opcode, byte[] data)
        {
            byte[] messageBytes = new byte[GetHeaderSize() + data.Length + 1];
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(data, 0, messageBytes, header.Length, data.Length); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }

        public int GetHeaderSize()
        {
            return ProtocolConstants.HEADER_SIZE;
        }
        
        public byte GetNullTerminator()
        {
            return ProtocolConstants.NULL_TERMINATOR;
        }

        public bool GetServerDone(byte[] Data, out byte Status)
        {
            try
            {
                Status = Data[0];
                return true;
            }
            catch
            {
                Status = 0;
                return false;
            }
        }
        
        public bool GetClientInitParams(byte[] Data, out int operation ,out int participants)
        {
            try
            {
                operation = BitConverter.ToInt32(Data, 0);
                participants = BitConverter.ToInt32(Data, sizeof(int));
                return true;
            }
            catch
            {
                operation = 0;
                participants = 0;
                return false;
            }
        }

        public bool GetServerInitParams(byte[] data, out string sessionId, out int operation, out int participants)
        {
            try
            {
                sessionId = Encoding.Default.GetString(data, 0, ProtocolConstants.SESSION_ID_SIZE);
                operation = BitConverter.ToInt32(data, ProtocolConstants.SESSION_ID_SIZE);
                participants = BitConverter.ToInt32(data, ProtocolConstants.SESSION_ID_SIZE+sizeof(int));
                return true;
            }
            catch
            {
                sessionId = string.Empty;
                operation = 0;
                participants = 0;
                return false;
            }
        }
        // sessionId, elementsCounter(32b), data
        public bool GetDataParams(byte[] data, out string Session, out UInt32 ElementsCounter, out List<uint> Elements)
        {
            try
            {
                //Session = BitConverter.ToString(Data, 0, ProtocolConstants.SESSION_ID_SIZE * sizeof(char));
                //byte[] sessionTemp = new byte[ProtocolConstants.SESSION_ID_SIZE];
                //)Buffer.BlockCopy(Data, 0, sessionTemp, 0, ProtocolConstants.SESSION_ID_SIZE);
                Session = Encoding.Default.GetString(data, 0, ProtocolConstants.SESSION_ID_SIZE);
                ElementsCounter = BitConverter.ToUInt32(data, ProtocolConstants.SESSION_ID_SIZE);
                Elements = MPCConvertor.BytesToList(data, ProtocolConstants.SESSION_ID_SIZE + sizeof(UInt32));
                return true;
            }
            catch
            {
                Session = string.Empty;
                ElementsCounter = 0;
                Elements = null;
                return false;
            }
        }

        public bool GetExchangeData(byte[] data, out uint[] exchangeData)
        {
            try
            {
                exchangeData = MPCConvertor.BytesToList(data, 0).ToArray();
                return true;
            }
            catch
            {
                exchangeData = null;
                return false;
            }
        }

    }
}
