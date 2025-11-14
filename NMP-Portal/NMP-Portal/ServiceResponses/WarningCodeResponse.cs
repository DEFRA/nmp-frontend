using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class WarningCodeResponse
    {
        [JsonProperty("fieldId")]
        public int FieldId { get; set; }

        [JsonProperty("warningCode")]
        public string WarningCode { get; set; }
    }
}
