using Newtonsoft.Json;
using NMP.Commons.Models;
namespace NMP.Commons.ServiceResponses;
public class UserFarmResponse
{
    [JsonProperty("Farms")]
    public List<Farm> Farms { get; set; }
}
