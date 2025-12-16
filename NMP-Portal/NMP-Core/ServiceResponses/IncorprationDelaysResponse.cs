using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class IncorprationDelaysResponse
{
    [JsonProperty("id")]
    public int? ID { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("fromHours")]
    public int? FromHours { get; set; }

    [JsonProperty("toHours")]
    public string? ToHours { get; set; }

    [JsonProperty("applicableFor")]
    public string? ApplicableFor { get; set; }
}
