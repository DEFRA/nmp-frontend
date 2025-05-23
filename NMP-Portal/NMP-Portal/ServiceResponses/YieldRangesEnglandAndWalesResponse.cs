using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class YieldRangesEnglandAndWalesResponse
    {
        [JsonProperty("yieldId")]
        public int YieldId { get; set; }

        [JsonProperty("yieldText")]
        public string YieldText { get; set; }
    }
    
}
