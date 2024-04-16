using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FarmResponseWrapper
    {
        [JsonProperty("Farm")]
        public FarmResponse? Farm { get; set; }
    }
}
