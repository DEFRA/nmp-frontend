using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
public class BankSlopeAnglesResponse
{
    [JsonProperty("Id")]
    public int ID { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("angle")]
    public decimal Angle { get; set; }

    [JsonProperty("slope")]
    public decimal Slope { get; set; }
}
