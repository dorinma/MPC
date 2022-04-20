using Newtonsoft.Json;

namespace MPCProtocol.Requests
{
    public class SortRandomRequest
    {
        [JsonProperty("sessionId", Required = Required.Always)]
        public string sessionId { get; set; }

        [JsonProperty("n", Required = Required.Always)]
        public int n { get; set; }

        [JsonProperty("dcfMasks", Required = Required.Always)]
        public ulong[] dcfMasks { get; set; }

        [JsonProperty("dcfKeys", Required = Required.Always)]
        public ulong[] dcfKeys { get; set; }

        [JsonProperty("dpfMasks", Required = Required.Always)]
        public ulong[] dpfMasks { get; set; }

        [JsonProperty("dpfKeys", Required = Required.Always)]
        public ulong[] dpfKeys { get; set; }
    }
}
