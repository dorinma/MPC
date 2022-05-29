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

        public T DeserializeRequest<T>(string requestString)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(requestString) ?? default;
            }
            catch
            {
                return default;
            }
        }

        public bool ValidateMessage(string prefix)
        {
            return prefix.Equals("MC");
        }
    }
}
