﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class SetMerchantDataReq : MerchantData
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.SET_MERCHANT_DATA_REQUEST;
    }
}
