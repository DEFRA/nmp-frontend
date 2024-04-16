using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FieldResponse
    {
        [JsonProperty("Count")]
        public int Count { get; set; }
    }
}
