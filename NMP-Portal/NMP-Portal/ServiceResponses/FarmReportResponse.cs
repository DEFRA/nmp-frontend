using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class FarmReportResponse : Farm
    {
        public int? GrassArea {  get; set; }
        public int? ArableArea { get; set; }
        [JsonProperty("Fields")]
        public List<FieldAndCropReportResponse>? Fields { get; set; }

    }
}
