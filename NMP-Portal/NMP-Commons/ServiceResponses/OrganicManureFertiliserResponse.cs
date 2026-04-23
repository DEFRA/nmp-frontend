using Newtonsoft.Json;
using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class OrganicManureFertiliserResponse
    {
        [JsonProperty("fertiliserManures")]
        public List<FertiliserManure>? FertiliserManures { get; set; }

        [JsonProperty("organicManures")]
        public List<OrganicManure>? OrganicManures { get; set; }
    }
}
