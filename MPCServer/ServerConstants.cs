using MPCProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    public enum SERVER_STATE
    {
        OFFLINE = 1,
        FIRST_INIT = 2,
        CONNECT_AND_DATA = 3,
        DATA = 4,
        COMPUTATION = 5
    }

    public class ServerConstants
    {
        public static Dictionary<OPCODE_MPC, SERVER_STATE> statesMap = new Dictionary<OPCODE_MPC, SERVER_STATE>
        {
            { OPCODE_MPC.E_OPCODE_RANDOM_SORT, SERVER_STATE.FIRST_INIT },
            { OPCODE_MPC.E_OPCODE_CLIENT_INIT, SERVER_STATE.FIRST_INIT },
            { OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT, SERVER_STATE.FIRST_INIT },
            { OPCODE_MPC.E_OPCODE_CLIENT_DATA, SERVER_STATE.CONNECT_AND_DATA },
        };

        public const string MSG_VALIDATE_PROTOCOL_FAIL = "Could not parse message.";
        public const string MSG_VALIDATE_SERVER_STATE_FAIL = "The server is currently not accepting this kind of messages.";
        public const string MSG_SESSION_RUNNING = "Session already running.";
        public const string MSG_VALIDATE_PARAMS_FAIL = "Could not parse message parameters.";
        public const string MSG_WRONG_SESSION_ID = "Session id is wrong.";
        public const string MSG_ALL_USERS_CONNECTED = "All users are connected";
    }
}
