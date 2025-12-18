using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class GrassGrowthClassResponse
{
    [JsonProperty("grassGrowthClassId")]
    public int GrassGrowthClassId { get; set; }

    [JsonProperty("grassGrowthClassName")]
    public string GrassGrowthClassName { get; set; }

    [JsonProperty("fieldId")]
    public int FieldId { get; set; }
}
