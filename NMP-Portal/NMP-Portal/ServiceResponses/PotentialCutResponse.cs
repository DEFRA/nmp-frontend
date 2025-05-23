using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class PotentialCutResponse
    {
        [JsonProperty("potentialCutId")]
        public int PotentialCutId { get; set; }

        [JsonProperty("potentialCutText")]
        public string PotentialCutText { get; set; }
    }
}
