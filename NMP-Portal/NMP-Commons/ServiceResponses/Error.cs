using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class Error
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("stack")]
    public string Stack { get; set; } = string.Empty;

    [JsonProperty("path")]
    public string Path { get; set; } = string.Empty;
}
