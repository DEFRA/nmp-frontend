using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class WarningResponse
    {
        [JsonProperty("id")]
        public int? ID { get; set; }

        [JsonProperty("warningKey")]
        public string WarningKey { get; set; }

        [JsonProperty("countryID")]
        public int CountryID { get; set; }

        [JsonProperty("header")]
        public string? Header { get; set; }

        [JsonProperty("para1")]
        public string? Para1 { get; set; }

        [JsonProperty("para2")]
        public string? Para2 { get; set; }

        [JsonProperty("para3")]
        public string? Para3 { get; set; }

        [JsonProperty("warningCodeID")]
        public int WarningCodeID { get; set; }

        [JsonProperty("warningLevelID")]
        public int WarningLevelID { get; set; }

    }
}
