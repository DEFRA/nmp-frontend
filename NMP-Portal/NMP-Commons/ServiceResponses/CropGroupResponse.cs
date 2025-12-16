using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class CropGroupResponse
{
    [JsonProperty("cropGroupId")]
    public int CropGroupId { get; set; }

    [JsonProperty("cropGroupName")]
    public string CropGroupName { get; set; }

    [JsonProperty("countryId")]
    public int CountryId { get; set; }
}
