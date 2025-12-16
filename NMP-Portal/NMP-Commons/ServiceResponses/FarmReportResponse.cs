using Newtonsoft.Json;
using NMP.Commons.Models;
namespace NMP.Commons.ServiceResponses;

public class FarmReportResponse  : Farm
{
    public int? GrassArea {  get; set; }
    public int? ArableArea { get; set; }
    [JsonProperty("Fields")]
    public List<FieldAndCropReportResponse>? Fields { get; set; }
}
