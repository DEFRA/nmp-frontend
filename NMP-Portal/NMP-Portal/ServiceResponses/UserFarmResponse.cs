using Newtonsoft.Json;
using NMP.Portal.Models;

namespace NMP.Portal.ServiceResponses
{
    public class UserFarmResponse
    {
        [JsonProperty("Farms")]
        public List<Farm> Farms { get; set; }
    }
}
