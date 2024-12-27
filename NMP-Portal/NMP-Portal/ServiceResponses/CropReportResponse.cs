using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class CropReportResponse: CropViewModel
    {
        [JsonProperty("ManagementPeriods")]
        public List<RecommendationReportResponse>? ManagementPeriods { get; set; }
    }
}
