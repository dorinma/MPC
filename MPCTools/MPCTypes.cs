using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

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
        public const int pendingQueueLength = 10;

        public const int portServerA = 2022;
        public const int portServerB = 2023;
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 30000;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }

    public enum OPCODE_MPC : UInt16
    {
        E_OPCODE_ERROR = 0xFF,

        E_OPCODE_CLIENT_INIT = 0x01,
        E_OPCODE_SERVER_INIT = 0x02,
        E_OPCODE_CLIENT_DATA = 0x03,
        E_OPCODE_SERVER_MSG = 0x04,
        E_OPCODE_SERVER_DATA = 0x05,
        E_OPCODE_SERVER_TO_SERVER_INIT = 0x06,
        E_OPCODE_RANDOM_SORT = 0x07,
        E_OPCODE_SERVER_VERIFY = 0x08,
        E_OPCODE_EXCHANGE_DATA = 0x09,
    }

    public enum OPERATION : UInt16
    {
        Sort = 0x01
    }

    public class Operations
    {
        public static readonly OPERATION[] operations = { OPERATION.Sort };
    }
}
