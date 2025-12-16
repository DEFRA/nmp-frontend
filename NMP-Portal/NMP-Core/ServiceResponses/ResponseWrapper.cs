using System.Text.Json.Serialization;

namespace NMP.Core.ServiceResponses;
public class ResponseWrapper
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("data")]
    public dynamic? Data { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("error")]
    public dynamic? Error { get; set; }
}
