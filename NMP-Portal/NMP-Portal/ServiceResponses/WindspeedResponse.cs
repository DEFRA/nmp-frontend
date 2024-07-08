using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class WindspeedResponse
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fromScale")]
        public int FromScale { get; set; }

        [JsonProperty("toScale")]
        public int ToScale { get; set; }

        
    }
}
