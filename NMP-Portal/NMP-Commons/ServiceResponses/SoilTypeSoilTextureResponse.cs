using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class SoilTypeSoilTextureResponse
{
    [JsonProperty("soilTypeID")]
    public int SoilTypeID { get; set; }

    [JsonProperty("topSoilID")]
    public int TopSoilID { get; set; }

    [JsonProperty("subSoilID")]
    public int SubSoilID { get; set; }
}
