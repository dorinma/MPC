using System;
using System.Threading;
using System.ComponentModel;

namespace MPCProtocol
{
    class ConstProtocol
    {
        public const int HEADER_SIZE = 4;

        public const byte SYNC_LSB = 0; //M
        public const byte SYNC_MSB = 1; //C
        public const byte OPCODE_LSB = 2;
    }

    public enum OPCODE_MPC : UInt16
    {
        E_OPCODE_ERROR          = 0xFF,
        
        E_OPCODE_INIT           = 0x01,
        E_OPCODE_DATA           = 0x02,
        E_OPCODE_SERVER_DONE    = 0x03  
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

        public void ParseData(byte[] Data, out UInt16 Opcode, out byte[] MsgData)
        {
            Opcode = BitConverter.ToUInt16(Data, ConstProtocol.OPCODE_LSB);
            MsgData = new byte[Data.Length - ConstProtocol.HEADER_SIZE];
            for (int i = 0; i < MsgData.Length; i++)
                MsgData[i] = Data[i + ConstProtocol.HEADER_SIZE];
        }


        public bool ValidateMessage(byte[] Data)
        {
            return Data[0] == 'M' && Data[1] == 'C';
        }

        public byte[] CreateHeaderDataMsg()
        {
            return new byte[] { (byte)'M', (byte)'C', 0, (byte)OPCODE_MPC.E_OPCODE_DATA };
        }

        public int GetHeaderSize()
        {
            return ConstProtocol.HEADER_SIZE;
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
        public bool GetInitParams(byte[] Data, out byte Participants, out byte InputsCount)
        {
            try
            {
                Participants = Data[0];
                InputsCount = Data[1];
                return true;
            }
            catch
            {
                Participants = 0;
                InputsCount = 0;
                return false;
            }
        }
    }
}
