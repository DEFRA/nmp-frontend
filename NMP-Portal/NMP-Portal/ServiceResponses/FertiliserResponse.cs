using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FertiliserResponse
    {

        [JsonProperty("ID")]
        public int? Id { get; set; }
        [JsonProperty("Name")]
        public string? Name { get; set; }
        [JsonProperty("FertiliserId")]
        public int? FertiliserId { get; set; }
        [JsonProperty("ManagementPeriodID")]
        public int? ManagementPeriodId { get; set; }
    }
}
