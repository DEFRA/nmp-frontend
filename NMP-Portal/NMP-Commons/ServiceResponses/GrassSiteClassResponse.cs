using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class GrassSiteClassResponse
    {
        [JsonProperty("siteClassId")]
        public int SiteClassId { get; set; }

        [JsonProperty("siteClass")]
        public string SiteClass { get; set; } = string.Empty;

        [JsonProperty("fieldId")]
        public int FieldId { get; set; }
    }
}
