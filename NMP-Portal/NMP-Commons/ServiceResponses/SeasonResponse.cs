using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class SeasonResponse
{
    [JsonProperty("seasonId")]
    public int SeasonId { get; set; }

    [JsonProperty("season")]
    public string Season { get; set; }

    [JsonProperty("countryId")]
    public int CountryId { get; set; }
}
