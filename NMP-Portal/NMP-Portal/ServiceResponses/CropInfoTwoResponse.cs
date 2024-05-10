using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class CropInfoTwoResponse
    {
        [JsonProperty("cropInfo2Id")]
        public int CropInfo2Id { get; set; }

        [JsonProperty("cropInfo2")]
        public string CropInfo2 { get; set; }

        [JsonProperty("countryId")]
        public int CountryId { get; set; }
    }
}
