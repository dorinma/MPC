using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class ClientInitRequest
    {
        [JsonProperty("operation", Required = Required.Always)]
        public OPERATION operation { get; set; }

        [JsonProperty("numberOfUsers", Required = Required.Always)]
        public int numberOfUsers { get; set; }

        [JsonProperty("debugMode", Required = Required.Always)]
        public bool debugMode { get; set; }
    }
}
