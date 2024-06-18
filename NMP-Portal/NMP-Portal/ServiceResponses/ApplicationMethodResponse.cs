using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class ApplicationMethodResponse
    {
        [JsonProperty("id")]
        public int? ID { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("applicableFor")]
        public string? ApplicableFor { get; set; }

    }
}
