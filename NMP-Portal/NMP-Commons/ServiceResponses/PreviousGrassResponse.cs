using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class PreviousGrassResponse
    {
        [JsonProperty("previousGrassId")]
        public int? PreviousGrassId { get; set; }
        [JsonProperty("previousGrass")]
        public string? PreviousGrassName { get; set; }
        [JsonProperty("countryId")]
        public string? CountryId { get; set; }
    }
}
