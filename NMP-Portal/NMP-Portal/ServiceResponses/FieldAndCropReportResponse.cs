using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class FieldAndCropReportResponse : FieldViewModel
    {
        [JsonProperty("Crops")]
        public List<CropReportResponse>? Crops { get; set; }

        [JsonProperty("PreviousCroppings")]
        public List<PreviousCropping>? PreviousCroppings { get; set; }

        [JsonProperty("SoilAnalysis")]
        public List<SoilAnalysisForReportResponse>? SoilAnalysis { get; set; }

         [JsonProperty("SoilDetails")]
        public SoilDetailsResponse? SoilDetails { get; set; }
    }
}
