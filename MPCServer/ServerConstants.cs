using MPCTools;
using System.Collections.Generic;

namespace MPCServer
{
    public enum SERVER_STATE
    {
        INIT = 1,
        CONNECT_AND_DATA = 2,
        COMPUTATION = 3,
        //DATA = 4,
    }

    public class ServerConstants
    {
        public static Dictionary<OPCODE_MPC, SERVER_STATE> statesMap = new Dictionary<OPCODE_MPC, SERVER_STATE>
        {
            { OPCODE_MPC.E_OPCODE_RANDOM_SORT, SERVER_STATE.INIT },
            { OPCODE_MPC.E_OPCODE_CLIENT_INIT, SERVER_STATE.INIT },
            { OPCODE_MPC.E_OPCODE_SERVER_TO_SERVER_INIT, SERVER_STATE.INIT },
            { OPCODE_MPC.E_OPCODE_CLIENT_DATA, SERVER_STATE.CONNECT_AND_DATA},
            { OPCODE_MPC.E_OPCODE_EXCHANGE_DATA, SERVER_STATE.COMPUTATION}
        };

        public const string debugRuleName = "debugRule";
        public const int RETRY_TIME = 10; // Minutes
        public const int SESSION_TIME = 15; // Minutes

        public const string MSG_BAD_MESSAGE_FORMAT = "Bad message format: Failed to parse request {0}";
        public const string MSG_VALIDATE_PROTOCOL_FAIL = "Bad mesage format, missing required prefix.";
        public const string MSG_VALIDATE_SERVER_STATE_FAIL = "The server is currently not accepting this kind of messages. Server state is {0}, Message opcode is {1}.";
        public const string MSG_SESSION_RUNNING = "Session already running.";
        public const string MSG_VALIDATE_PARAMS_FAIL = "Could not parse message parameters.";
        public const string MSG_WRONG_SESSION_ID = "Session id is wrong.";
        public const string MSG_ALL_USERS_CONNECTED = "All users are connected";
    }
}
