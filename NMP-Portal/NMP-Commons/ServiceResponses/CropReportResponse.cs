using Newtonsoft.Json;
using NMP.Commons.ViewModels;
namespace NMP.Commons.ServiceResponses;

public class CropReportResponse : CropViewModel
{
    [JsonProperty("ManagementPeriods")]
    public List<RecommendationReportResponse>? ManagementPeriods { get; set; }
    [JsonProperty("SNSAnalysis")]
    public SnsResponse? SNSAnalysis { get; set; }
}
