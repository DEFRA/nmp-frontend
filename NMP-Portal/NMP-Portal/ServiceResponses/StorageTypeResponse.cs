using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class StorageTypeResponse
    {
        [JsonProperty("Id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("freeBoardHeight")]
        public decimal FreeBoardHeight { get; set; }
    }
}
