using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;

//TODO: Need to review FieldViewModel inheritance
public class FieldAndCropReportResponse // : FieldViewModel
{
    [JsonProperty("Crops")]
    public List<CropReportResponse>? Crops { get; set; }

    //[JsonProperty("PreviousCroppings")]
    //public List<PreviousCropping>? PreviousCroppings { get; set; }

    [JsonProperty("SoilAnalysis")]
    public List<SoilAnalysisForReportResponse>? SoilAnalysis { get; set; }

     [JsonProperty("SoilDetails")]
    public SoilDetailsResponse? SoilDetails { get; set; }
}
