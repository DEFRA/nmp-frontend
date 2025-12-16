using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;

//TODO Need to review CropViewModel inheritance here
public class CropReportResponse //: CropViewModel
{
    [JsonProperty("ManagementPeriods")]
    public List<RecommendationReportResponse>? ManagementPeriods { get; set; }
    [JsonProperty("SNSAnalysis")]
    public SnsResponse? SNSAnalysis { get; set; }
}
