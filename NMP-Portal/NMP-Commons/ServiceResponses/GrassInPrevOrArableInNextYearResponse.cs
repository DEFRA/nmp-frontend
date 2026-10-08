using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class GrassInPrevOrArableInNextYearResponse
    {
        [JsonProperty("fieldId")]
        public int? FieldId { get; set; }

        [JsonProperty("isGrassInPrevYear")]
        public bool? IsGrassInPrevYear { get; set; }

        [JsonProperty("isArableInNextYear")]
        public bool? IsArableInNextYear { get; set; }
    }
}
