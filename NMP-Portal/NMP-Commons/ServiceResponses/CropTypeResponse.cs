using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class CropTypeResponse
{
    [JsonProperty("cropTypeId")]
    public int CropTypeId { get; set; }

    [JsonProperty("cropType")]
    public string CropType { get; set; }

    [JsonProperty("cropGroupId")]
    public int CropGroupId { get; set; }

    [JsonProperty("countryId")]
    public int CountryId { get; set; }
}
