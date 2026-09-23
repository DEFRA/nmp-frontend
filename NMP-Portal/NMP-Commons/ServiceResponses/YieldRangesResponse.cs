using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class YieldRangesResponse
{
    [JsonProperty("yieldId")]
    public int YieldId { get; set; }

    [JsonProperty("yieldText")]
    public string YieldText { get; set; }
}

