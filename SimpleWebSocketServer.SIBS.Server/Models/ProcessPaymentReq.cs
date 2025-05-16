﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class ProcessPaymentReq
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.PROCESS_PAYMENT_REQUEST;

        [JsonProperty("amountData")]
        public AmountData AmountData { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; } = System.Guid.NewGuid().ToString().Substring(0, 20);
    }
}
