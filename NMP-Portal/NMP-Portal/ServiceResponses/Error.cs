using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class Error
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("stack")]
        public string Stack { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
