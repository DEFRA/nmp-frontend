using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class GrassSeasonResponse
{
    [JsonProperty("seasonId")]
    public int SeasonId { get; set; }

    [JsonProperty("seasonName")]
    public string SeasonName { get; set; }
}
