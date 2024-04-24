using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FieldResponseWapper
    {
        [JsonProperty("nutrientId")]
        public int nutrientId { get; set; }
        [JsonProperty("nutrient")]
        public string nutrient { get; set; } = string.Empty;
    }
}
