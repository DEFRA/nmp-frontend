using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class RainTypeResponse
{
    [JsonProperty("id")]
    public int ID { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("rainInMM")]
    public int RainInMM { get; set; }
}
