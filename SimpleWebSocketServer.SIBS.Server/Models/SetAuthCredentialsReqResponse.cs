﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class SetAuthCredentialsReqResponse
    {
        [JsonProperty("resultStatus")]
        public string ResultStatus { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.SET_AUTH_CREDENTIAL_RESPONSE;

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
