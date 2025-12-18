using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class NitrogenUptakeResponse
{
    [JsonProperty("value")]
    public int value { get; set; }

    [JsonProperty("unit")]
    public string unit { get; set; } = string.Empty;
}
