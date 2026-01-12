using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class WarningHeaderResponse
{
    [JsonProperty("fieldId")]
    public int FieldId { get; set; }

    [JsonProperty("warningHeader")]
    public string WarningHeader { get; set; }
}
