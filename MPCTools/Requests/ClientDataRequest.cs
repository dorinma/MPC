using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class ClientDataRequest
    {
        [JsonProperty("sessionId", Required = Required.Always)]
        public string sessionId { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public uint[] dataShares { get; set; }
    }
}
