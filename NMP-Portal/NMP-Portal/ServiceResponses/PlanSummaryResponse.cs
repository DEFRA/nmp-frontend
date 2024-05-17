using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class PlanSummaryResponse
    {
        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("lastModifiedOn")]
        public DateTime LastModifiedOn { get; set; }
        
    }
}
