using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class FieldAndCropReportResponse :FieldViewModel
    {
        [JsonProperty("Crops")]

        public List<CropReportResponse>? Crops { get; set; }
        [JsonProperty("PreviousGrasses")]
        public List<PreviousGrass>? PreviousGrasses { get; set; }
        [JsonProperty("SoilAnalysis")]
        public SoilAnalysisReportResponse? SoilAnalysis { get; set; }
        [JsonProperty("PKBalance")]
        public PKBalance? PKBalance { get; set; }
    }
}
