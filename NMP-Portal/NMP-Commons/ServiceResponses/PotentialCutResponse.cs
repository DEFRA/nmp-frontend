using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class PotentialCutResponse
{
    [JsonProperty("potentialCutId")]
    public int PotentialCutId { get; set; }

    [JsonProperty("potentialCutText")]
    public string PotentialCutText { get; set; }
}
