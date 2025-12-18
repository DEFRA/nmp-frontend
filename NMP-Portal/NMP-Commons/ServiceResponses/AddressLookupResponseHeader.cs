using Newtonsoft.Json;

namespace NMP.Commons.ServiceResponses
{
    public class AddressLookupResponseHeader
    {
        [JsonProperty("query")]
        public string Query { get; set; }= string.Empty;

        [JsonProperty("offset")]
        public string Offset { get; set; } = string.Empty;

        [JsonProperty("totalResults")]
        public string TotalResults { get; set; } = string.Empty;

        [JsonProperty("format")]
        public string Format { get; set; } = string.Empty;

        [JsonProperty("dataset")]
        public string Dataset { get; set; } = string.Empty;

        [JsonProperty("language")]
        public string Language { get; set; } = string.Empty;

        [JsonProperty("maximumResults")]
        public string MaximumResults { get; set; } = string.Empty;

        [JsonProperty("matchingTotalResults")]
        public string MatchingTotalResults { get; set; } = string.Empty;
    }
}
