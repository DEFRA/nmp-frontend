using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class DefoliationSequenceResponse
{
    [JsonProperty("defoliationSequenceId")]
    public int DefoliationSequenceId { get; set; }

    [JsonProperty("defoliationSequence")]
    public string DefoliationSequence { get; set; } = string.Empty;

    [JsonProperty("defoliationSequenceDescription")]
    public string DefoliationSequenceDescription { get; set; } = string.Empty;
}
