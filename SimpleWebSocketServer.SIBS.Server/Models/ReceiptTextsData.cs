using Newtonsoft.Json;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class ReceiptTextsData
    {
        [JsonProperty("acquirerText")]
        public string AcquirerText { get; set; }
    }
}
