using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class NewTerminalConnectedReq
    {
        [JsonProperty("deviceSerialNumber")]
        public System.Guid ClientId { get; set; }


        [JsonProperty("terminalId")]
        public int TerminalId { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.NEW_TERMINAL_CONNECTED_REQUEST;

        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
