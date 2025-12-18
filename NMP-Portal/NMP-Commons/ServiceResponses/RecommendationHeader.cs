using Newtonsoft.Json;
using NMP.Commons.Models;
using NMP.Commons.ViewModels;
namespace NMP.Commons.ServiceResponses;

public class RecommendationHeader
{
    [JsonProperty("Crop")]
    public CropViewModel? Crops { get; set; }
    [JsonProperty("PKbalance")]
    public PKBalance? PKBalance { get; set; }
    [JsonProperty("Recommendations")]
    public List<RecommendationData>? RecommendationData { get; set; }
}
