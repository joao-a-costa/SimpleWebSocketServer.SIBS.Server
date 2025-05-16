using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static SimpleWebSocketServer.SIBS.Server.Enums.Enums;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class LinqTerminalToFrontResponse
    {
        [JsonProperty("version")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Version Version { get; set; } = Version.V_1;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RequestType Type { get; set; } = RequestType.LINQ_TERMINAL_TO_FRONT_RESPONSE;

        [JsonProperty("terminal")]
        public int Terminal { get; set; }

        [JsonProperty("front")]
        public System.Guid Front { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; } = false;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

    }
}
