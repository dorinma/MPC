using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class DataRequest
    {
        [JsonProperty("sessionId", Required = Required.Always)]
        public string sessionId { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public uint[] dataElements { get; set; }
    }
}
