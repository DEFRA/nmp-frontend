using Newtonsoft.Json;
using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class RecommendationHeader
    {
        [JsonProperty("Crop")]
        public Crop? Crops { get; set; }
        [JsonProperty("PKbalance")]

        public PKBalance? PKBalance { get; set; }
        [JsonProperty("Recommendations")]
        public List<RecommendationData>? RecommendationData { get; set; }
    }
}
