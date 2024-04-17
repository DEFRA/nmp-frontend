using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FieldResponseWapper
    {
        [JsonProperty("Count")]
        public int Count { get; set; }
    }
}
