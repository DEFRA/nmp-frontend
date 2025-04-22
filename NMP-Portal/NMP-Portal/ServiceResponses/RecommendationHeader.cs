using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class RecommendationHeader
    {
        [JsonProperty("Crop")]
        public CropViewModel? Crops { get; set; }
        [JsonProperty("PKbalance")]

        public PKBalance? PKBalance { get; set; }
        [JsonProperty("Recommendations")]
        public List<RecommendationData>? RecommendationData { get; set; }
    }
}
