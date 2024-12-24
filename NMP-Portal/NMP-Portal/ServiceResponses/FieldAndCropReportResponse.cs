using Newtonsoft.Json;
using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class FieldAndCropReportResponse :Field
    {
        //[JsonProperty("Fields")]
        //public Field Fields { get; set; }

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
