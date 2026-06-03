using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class ReportYearLastUpdatedDateResponse
    {
        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("lastUpdatedDate")]
        public string? LastUpdatedDate { get; set; }
    }
}
