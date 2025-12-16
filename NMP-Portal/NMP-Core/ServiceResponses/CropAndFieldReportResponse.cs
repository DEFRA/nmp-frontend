using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class CropAndFieldReportResponse
{
    [JsonProperty("Farm")]
    public FarmReportResponse? Farm { get; set; }
    public string? ExcessWinterRainfall { get; set; }
    
}
