using Newtonsoft.Json;
using NMP.Commons.Models;
namespace NMP.Core.ServiceResponses;
public class UserFarmResponse
{
    [JsonProperty("Farms")]
    public List<Farm> Farms { get; set; }
}
