using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class YieldRangesEnglandAndWalesResponse
    {
        [JsonProperty("yield")]
        public int Yield { get; set; }
    }
}
