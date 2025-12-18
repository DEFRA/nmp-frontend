using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class WarningCodeResponse
{
    [JsonProperty("fieldId")]
    public int FieldId { get; set; }

    [JsonProperty("warningCode")]
    public string WarningCode { get; set; }
}
