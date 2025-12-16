using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class PlanSummaryResponse
{
    [JsonProperty("year")]
    public int Year { get; set; }

    [JsonProperty("lastModifiedOn")]
    public DateTime LastModifiedOn { get; set; }

}
