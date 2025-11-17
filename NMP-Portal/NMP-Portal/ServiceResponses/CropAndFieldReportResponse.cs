using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class CropAndFieldReportResponse
    {

        [JsonProperty("Farm")]
        public FarmReportResponse? Farm { get; set; }
        public string? ExcessWinterRainfall { get; set; }
        //[JsonProperty("Fields")]
        //public List<FieldAndCropReportResponse>? Fields { get; set; }
    }
}
