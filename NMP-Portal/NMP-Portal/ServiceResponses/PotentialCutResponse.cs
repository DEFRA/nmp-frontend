using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class PotentialCutResponse
    {
        [JsonProperty("potentialCut")]
        public string PotentialCut { get; set; }
    }
}
