using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class DefoliationSequenceResponse
{
    [JsonProperty("defoliationSequenceId")]
    public int DefoliationSequenceId { get; set; }

    [JsonProperty("defoliationSequence")]
    public string DefoliationSequence { get; set; }

    [JsonProperty("defoliationSequenceDescription")]
    public string DefoliationSequenceDescription { get; set; }
}
