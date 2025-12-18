using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class ManureCropTypeResponse
{
    [JsonProperty("CropTypeID")]
    public int CropTypeId { get; set; }

    [JsonProperty("CropTypeName")]
    public string CropType { get; set; }

    [JsonProperty("cropOrder")]
    public string CropOrder { get; set; }
    [JsonProperty("CropGroupName")]
    public string CropGroupName { get; set; }
}
