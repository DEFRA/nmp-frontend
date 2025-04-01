using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FertiliserAndManureSameDateAndNutrientValueResponse
    {

        [JsonProperty("ID")]
        public int? Id { get; set; }
        [JsonProperty("Name")]
        public string? Name { get; set; }
        [JsonProperty("FertiliserId")]
        public int? FertiliserId { get; set; }
        [JsonProperty("OrganicManureId")]
        public int? OrganicManureId { get; set; }
        [JsonProperty("ManagementPeriodID")]
        public int? ManagementPeriodId { get; set; }
    }
}
