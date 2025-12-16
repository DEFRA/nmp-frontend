using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class SwardTypeResponse
{
    [JsonProperty("swardTypeId")]
    public int SwardTypeId { get; set; }

    [JsonProperty("swardType")]
    public string SwardType { get; set; }
}
