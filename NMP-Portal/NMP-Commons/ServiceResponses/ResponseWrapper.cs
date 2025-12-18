using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class ResponseWrapper
{
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("status")]
    public bool Status { get; set; }

    [JsonProperty("data")]
    public dynamic? Data { get; set; }

    [JsonProperty("statusCode")]
    public int StatusCode { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonProperty("error")]
    public dynamic? Error { get; set; }
}
