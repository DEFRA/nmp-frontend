using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class OrganicManureFieldResponse
    {
        [JsonProperty("ID")]
        public int FieldId { get; set; }
        [JsonProperty("Name")]
        public string FieldName { get; set; }
    }
}
