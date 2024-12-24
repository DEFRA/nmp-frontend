using Newtonsoft.Json;
using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class CropReportResponse:Crop
    {
        //[JsonProperty("Crops")]
        //public List<Crop>? Crops { get; set; }
        [JsonProperty("ManagementPeriods")]
        public List<RecommendationReportResponse>? ManagementPeriods { get; set; }
    }
}
