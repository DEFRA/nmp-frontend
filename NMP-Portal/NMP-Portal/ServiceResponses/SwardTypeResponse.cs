using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class SwardTypeResponse
    {
        [JsonProperty("swardTypeId")]
        public int SwardTypeId { get; set; }

        [JsonProperty("swardType")]
        public string SwardType { get; set; }
    }
}
