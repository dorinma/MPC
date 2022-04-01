using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MPCProtocol
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

    public enum OPCODE_MPC : UInt16
    {
        E_OPCODE_ERROR          = 0xFF,
        
        E_OPCODE_CLIENT_INIT    = 0x01,
        E_OPCODE_SERVER_INIT    = 0x02,
        E_OPCODE_CLIENT_DATA    = 0x03,
        E_OPCODE_SERVER_MSG     = 0x04,  
        E_OPCODE_SERVER_DATA     = 0x05 
    }

    public class Protocol
    {
        private static Protocol instance = null;

        protected AsyncOperation operation;

        public delegate void ServerDone(object sender, byte Status);
        public delegate void Init(object sender, byte Participants, byte InputsCount);

        public event ServerDone Event_ServerDone;
        public event Init Event_Init;

        private Protocol()
        {
        }

        public static Protocol Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Protocol();
                }
                return instance;
            }
        }

        static void Main(string[] args) 
        {
        }

        public void ParseData(byte[] Data, out OPCODE_MPC Opcode, out byte[] MsgData)
        {
            Opcode = (OPCODE_MPC)BitConverter.ToUInt16(Data, ProtocolConstants.OPCODE_LSB);
            MsgData = new byte[Data.Length - ProtocolConstants.HEADER_SIZE];
            for (int i = 0; i < MsgData.Length; i++)
                MsgData[i] = Data[i + ProtocolConstants.HEADER_SIZE];
        }


        public bool ValidateMessage(byte[] Data)
        {
            return Data[0] == 'M' && Data[1] == 'C';
        }

        public byte[] CreateHeaderDataMsg()
        {
            return new byte[] { (byte)'M', (byte)'C', (byte)OPCODE_MPC.E_OPCODE_CLIENT_DATA, 0 };
        }
        public byte[] CreateHeaderInitMsg()
        {
            return new byte[] { (byte)'M', (byte)'C', (byte)OPCODE_MPC.E_OPCODE_CLIENT_DATA, 0 };
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

        public byte[] CreateDataMessage(OPCODE_MPC opcode, string sessionId, int elementSize, Array data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] sessionBytes = Encoding.ASCII.GetBytes(sessionId);
            byte[] messageBytes = new byte[header.Length + sessionBytes.Length + elementSize * data.Length + 1];
            Buffer.BlockCopy(header, 0, messageBytes, 0, header.Length); //header
            Buffer.BlockCopy(sessionBytes, 0, messageBytes, header.Length, sessionBytes.Length); //header
            Buffer.BlockCopy(data, 0, messageBytes, header.Length + sessionBytes.Length, data.Length * elementSize); //data
            messageBytes[messageBytes.Length - 1] = GetNullTerminator();
            return messageBytes;
        }

        public byte[] CreateStringMessage(OPCODE_MPC opcode, string data)
        {
            var header = new byte[] { (byte)'M', (byte)'C', (byte)opcode, 0 };
            byte[] byteData = Encoding.ASCII.GetBytes(data);
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
        
        public bool GetInitParams(byte[] Data, out uint participants)
        {
            try
            {
                participants = BitConverter.ToUInt32(Data, 0);
                return true;
            }
            catch
            {
                participants = 0;
                return false;
            }
        }

        // sessionId, elementsCounter(32b), data
        public bool GetDataParams(byte[] Data, out string Session, out UInt32 ElementsCounter, out List<UInt16> Elements)
        {
            try
            {
                //Session = BitConverter.ToString(Data, 0, ProtocolConstants.SESSION_ID_SIZE * sizeof(char));
                byte[] sessionTemp = new byte[ProtocolConstants.SESSION_ID_SIZE * sizeof(char)];
                Buffer.BlockCopy(Data, 0, sessionTemp, 0, ProtocolConstants.SESSION_ID_SIZE * sizeof(char));
                Session = System.Text.Encoding.Default.GetString(sessionTemp);
                ElementsCounter = BitConverter.ToUInt32(Data, ProtocolConstants.SESSION_ID_SIZE);
                Elements = MPCConvertor.BytesToList(Data, ProtocolConstants.SESSION_ID_SIZE + sizeof(UInt32));
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
    }
}
