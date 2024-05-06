using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class PotatoVarietyResponse
    {
        [JsonProperty("potatoVarietyId")]
        public int PotatoVarietyId { get; set; }

        [JsonProperty("potatoGroupId")]
        public int PotatoGroupId { get; set; }

        [JsonProperty("potatoVariety")]
        public string PotatoVariety { get; set; }

        [JsonProperty("countryId")]
        public int CountryId { get; set; }
    }
}
