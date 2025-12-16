using System.Text.Json.Serialization;
namespace NMP.Core.ServiceResponses;
public class Info
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("dateTime")]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("nodeID")]
    public string NodeID { get; set; } = string.Empty;

    [JsonPropertyName("atomID")]
    public string AtomID { get; set; } = string.Empty;
}
