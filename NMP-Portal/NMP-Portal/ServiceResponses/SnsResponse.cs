using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class SnsResponse
    {
        [JsonProperty("snsValue")]
        public int SnsValue { get; set; }

        [JsonProperty("snsIndex")]
        public int SnsIndex { get; set; }

    }
}
