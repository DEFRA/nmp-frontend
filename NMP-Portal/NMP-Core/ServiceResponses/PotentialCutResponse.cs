using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class PotentialCutResponse
{
    [JsonProperty("potentialCutId")]
    public int PotentialCutId { get; set; }

    [JsonProperty("potentialCutText")]
    public string PotentialCutText { get; set; }
}
