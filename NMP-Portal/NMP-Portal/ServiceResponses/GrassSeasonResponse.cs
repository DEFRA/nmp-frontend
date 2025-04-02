using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class GrassSeasonResponse
    {
        [JsonProperty("seasonId")]
        public int SeasonId { get; set; }

        [JsonProperty("seasonName")]
        public string SeasonName { get; set; }

    }
}
