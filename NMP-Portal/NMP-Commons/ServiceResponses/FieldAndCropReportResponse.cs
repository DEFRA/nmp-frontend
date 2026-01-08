using Newtonsoft.Json;
using NMP.Commons.ViewModels;
using NMP.Commons.Models;
namespace NMP.Commons.ServiceResponses;

public class FieldAndCropReportResponse  : FieldViewModel
{
    [JsonProperty("Crops")]
    public List<CropReportResponse>? Crops { get; set; }

    [JsonProperty("PreviousCroppings")]
    public List<PreviousCropping>? PreviousCroppings { get; set; }

    [JsonProperty("SoilAnalysis")]
    public SoilAnalysisForReportResponse? SoilAnalysis { get; set; }

     [JsonProperty("SoilDetails")]
    public SoilDetailsResponse? SoilDetails { get; set; }
}
