using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class YieldRangesEnglandAndWalesResponse
{
    [JsonProperty("yieldId")]
    public int YieldId { get; set; }

    [JsonProperty("yieldText")]
    public string YieldText { get; set; }
}

