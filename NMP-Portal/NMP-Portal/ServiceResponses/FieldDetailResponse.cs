using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FieldDetailResponse
    {
        [JsonProperty("fieldType")]
        public int? FieldType { get; set; }

        [JsonProperty("soilTypeID")]
        public int? SoilTypeID { get; set; }

        [JsonProperty("soilTypeName")]
        public string SoilTypeName { get; set; }

        [JsonProperty("sowingDate")]
        public DateTime? SowingDate { get; set; }
    }
}
