using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class YieldRangesEnglandAndWalesResponse
{
    [JsonProperty("yieldId")]
    public int YieldId { get; set; }

    [JsonProperty("yieldText")]
    public string YieldText { get; set; }
}

