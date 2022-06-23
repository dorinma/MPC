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
                return JsonConvert.DeserializeObject<T>(requestString);
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
