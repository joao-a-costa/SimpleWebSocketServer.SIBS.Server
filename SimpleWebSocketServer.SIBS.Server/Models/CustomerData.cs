using Newtonsoft.Json;

namespace SimpleWebSocketServer.SIBS.Server.Models
{
    public class CustomerData
    {
        [JsonProperty("fiscalNumber")]
        public string FiscalNumber { get; set; }
        [JsonProperty("fiscalNumberCountryISO2Code")]
        public string FiscalNumberCountryISO2Code { get; set; }
    }
}