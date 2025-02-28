using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class ApplicationMethodResponse
    {
        [JsonProperty("id")]
        public int? ID { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("applicableForGrass")]
        public string? ApplicableForGrass { get; set; }

        [JsonProperty("applicableForArableAndHorticulture")]
        public string? ApplicableForArableAndHorticulture { get; set; }

        [JsonProperty("sortOrder")]
        public int SortOrder { get; set; }
        

    }
}
