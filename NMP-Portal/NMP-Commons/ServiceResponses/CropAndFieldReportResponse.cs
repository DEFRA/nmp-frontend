using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class CropAndFieldReportResponse
{
    [JsonProperty("Farm")]
    public FarmReportResponse? Farm { get; set; }
    public string? ExcessWinterRainfall { get; set; }
    
}
