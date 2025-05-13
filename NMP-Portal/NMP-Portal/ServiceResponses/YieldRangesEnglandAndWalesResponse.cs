using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class YieldRangesEnglandAndWalesResponse
    {
        [JsonProperty("yield")]
        public int Yield { get; set; }
    }

    //Temporary class for grass yield
    public class YieldRangesEnglandAndWalesResponseTemprary
    {
        [JsonProperty("yield")]
        public string Yield { get; set; }
    }
}
