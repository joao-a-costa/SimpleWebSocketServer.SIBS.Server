﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class GetMerchantDataReqResponse
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.GET_MERCHANT_DATA_RESPONSE;

        [JsonProperty("merchantData")]
        public MerchantData MerchantData { get; set; }
    }
}