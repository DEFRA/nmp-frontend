using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class OrganicManureResponse
    {
        [JsonProperty("ID")]
        public int ID { get; set; }
        [JsonProperty("TypeOfManure")]
        public string? TypeOfManure { get; set; }
        [JsonProperty("ApplicationDate")]
        public DateTime? ApplicationDate { get; set; }
        [JsonProperty("Field")]
        public string? Field { get; set; }
        [JsonProperty("Crop")]
        public string? Crop { get; set; }
        [JsonProperty("Rate")]
        public decimal? Rate { get; set; }
    }
}
