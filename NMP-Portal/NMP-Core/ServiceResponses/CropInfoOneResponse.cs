using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class CropInfoOneResponse
{
    [JsonProperty("cropInfo1Id")]
    public int CropInfo1Id { get; set; }

    [JsonProperty("cropInfo1Name")]
    public string CropInfo1Name { get; set; }

    [JsonProperty("cropTypeId")]
    public int CropTypeId { get; set; }

    [JsonProperty("countryId")]
    public int CountryId { get; set; }
}

