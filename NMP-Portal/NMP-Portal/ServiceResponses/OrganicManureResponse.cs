using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class OrganicManureResponse
    {
        [JsonProperty("OrganicMaterialId")]
        public int ID { get; set; }
        [JsonProperty("TypeOfManure")]
        public string? TypeOfManure { get; set; }
        [JsonProperty("ApplicationDate")]
        public DateTime? ApplicationDate { get; set; }
        [JsonProperty("Field")]
        public string? Field { get; set; }
        [JsonProperty("FieldId")]
        public string? FieldId { get; set; }
        [JsonProperty("Crop")]
        public string? Crop { get; set; }
        [JsonProperty("Rate")]
        public decimal? Rate { get; set; }
        public string? EncryptedId { get; set; }
        public string? EncryptedFieldName { get; set; }
    }
}
