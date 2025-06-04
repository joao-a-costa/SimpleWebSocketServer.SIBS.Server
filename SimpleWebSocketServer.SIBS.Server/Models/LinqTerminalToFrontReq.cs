using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class LinqTerminalToFrontReq
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.LINQ_TERMINAL_TO_FRONT_REQUEST;

        [JsonProperty("terminal")]
        public long Terminal { get; set; }

        [JsonProperty("front")]
        public System.Guid Front { get; set; }
    }
}
