using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class NitrogenUptakeResponse
    {
        [JsonPropertyName("value")]
        public int value { get; set; }

        [JsonPropertyName("unit")]
        public string unit { get; set; } = string.Empty;
    }
}
