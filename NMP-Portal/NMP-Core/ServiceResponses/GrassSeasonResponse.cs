using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class GrassSeasonResponse
{
    [JsonProperty("seasonId")]
    public int SeasonId { get; set; }

    [JsonProperty("seasonName")]
    public string SeasonName { get; set; }
}
