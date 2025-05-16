﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class RefundReq
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.REFUND_REQUEST;

        [JsonProperty("originalTransactionData")]
        public OriginalTransactionData OriginalTransactionData { get; set; }

        [JsonProperty("refundTransactionId")]
        public string RefundTransactionId { get; set; } = System.Guid.NewGuid().ToString().Substring(0, 20);

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; } = System.Guid.NewGuid().ToString().Substring(0, 20);
    }

    public class OriginalTransactionData
    {
        [JsonProperty("authorizationType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorizationType AuthorizationType { get; set; } = AuthorizationType.NA;

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }

        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }

        [JsonProperty("serverId")]
        public string ServerId { get; set; }

        [JsonProperty("sdkId")]
        public string SdkId { get; set; }

        [JsonProperty("serverDateTime")]
        public System.DateTime ServerDateTime { get; set; }

        [JsonProperty("dccInitiatorId")]
        public string DccInitiatorId { get; set; } = string.Empty;
    }
}
