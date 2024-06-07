using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class CommonResponse
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}
