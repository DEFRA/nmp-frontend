using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class AddressLookupResponseWrapper
    {
        [JsonProperty("header")]
        public AddressLookupResponseHeader? Header { get; set; }

        [JsonProperty("results")]
        public List<AddressLookupResponse>? Results { get; set; }

        [JsonProperty("_info")]
        public Info? Info { get; set; }
    }
}
