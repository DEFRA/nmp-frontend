using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;

//TODO: Need to review Farm model inheritance
public class FarmReportResponse // : Farm
{
    public int? GrassArea {  get; set; }
    public int? ArableArea { get; set; }
    [JsonProperty("Fields")]
    public List<FieldAndCropReportResponse>? Fields { get; set; }
}
