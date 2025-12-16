using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class SnsResponse
{
    [JsonProperty("snsValue")]
    public int SnsValue { get; set; }

    [JsonProperty("snsIndex")]
    public int SnsIndex { get; set; }
    [JsonProperty("SNSMethod")]
    public string? SNSMethod { get; set; }
}
