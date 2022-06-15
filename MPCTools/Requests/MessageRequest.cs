using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class MessageRequest
    {
        [JsonProperty("prefix", Required = Required.Always)]
        public string prefix { get; set; }

        [JsonProperty("opcode", Required = Required.Always)]
        public OPCODE_MPC opcode { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public string data { get; set; }

        [JsonProperty("suffix", Required = Required.Always)]
        public string suffix { get; set; }
    }
}
