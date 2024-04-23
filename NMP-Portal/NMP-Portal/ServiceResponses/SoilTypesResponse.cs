using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class SoilTypesResponse
    {
        [JsonProperty("soilTypeId")]
        public int SoilTypeId { get; set; }

        [JsonProperty("soilType")]
        public string SoilType { get; set; }

        [JsonProperty("kReleasingClay")]
        public bool KReleasingClay { get; set; }

        [JsonProperty("countryId")]
        public int CountryId { get; set; }

    }
}
