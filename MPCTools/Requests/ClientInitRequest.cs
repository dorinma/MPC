﻿using Newtonsoft.Json;

namespace MPCTools.Requests
{
    public class ClientInitRequest
    {
        [JsonProperty("operation", Required = Required.Always)]
        public int operation { get; set; }

        [JsonProperty("numberOfUsers", Required = Required.Always)]
        public int numberOfUsers { get; set; }
    }
}
