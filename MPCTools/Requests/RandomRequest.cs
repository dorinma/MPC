using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class RandomRequest
    {
        //op

        [JsonProperty("sessionId", Required = Required.Always)]
        public string sessionId { get; set; }

        [JsonProperty("operation", Required = Required.Always)]
        public OPERATION operation { get; set; }

        [JsonProperty("n", Required = Required.Always)]
        public int n { get; set; }

        [JsonProperty("dcfMasks", Required = Required.Always)]
        public uint[] dcfMasks { get; set; }

        [JsonProperty("dcfKeysSmallerLowerBound", Required = Required.Always)]
        public string[] dcfKeysSmallerLowerBound { get; set; }

        [JsonProperty("dcfKeysSmallerUpperBound", Required = Required.Always)]
        public string[] dcfKeysSmallerUpperBound { get; set; }

        [JsonProperty("shares01", Required = Required.Always)]
        public uint[] shares01 { get; set; }

        [JsonProperty("dcfAesKeysLower", Required = Required.Always)]
        public string[] dcfAesKeysLower { get; set; }

        [JsonProperty("dcfAesKeysUpper", Required = Required.Always)]
        public string[] dcfAesKeysUpper { get; set; }

        [JsonProperty("dpfMasks", Required = Required.Always)]
        public uint[] dpfMasks { get; set; }

        [JsonProperty("dpfKeys", Required = Required.Always)]
        public string[] dpfKeys { get; set; }

        [JsonProperty("dpfAesKeys", Required = Required.Always)]
        public string[] dpfAesKeys { get; set; }
    }
}
