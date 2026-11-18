using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class DefaultFreshWeightYieldResponse
{
    [JsonProperty("sequencePosition")]
    public int SequencePosition { get; set; }

    [JsonProperty("sequenceType")]
    public string SequenceType { get; set; } = string.Empty;

    [JsonProperty("sequenceTypeName")]
    public string SequenceTypeName { get; set; } = string.Empty;

    [JsonProperty("yield")]
    public decimal? Yield { get; set; }
}
