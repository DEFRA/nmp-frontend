using Newtonsoft.Json;
using NMP.Portal.Models;
using NMP.Portal.ViewModels;

namespace NMP.Portal.ServiceResponses
{
    public class FarmResponse
    {
        [JsonProperty("Farm")]
        public FarmViewModel? FarmViewModel { get; set; }
    }
}
