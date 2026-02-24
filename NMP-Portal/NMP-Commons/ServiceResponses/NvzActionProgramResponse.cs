using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class NvzActionProgramResponse
    {
        [JsonProperty("nvzId")]
        public int NvzId { get; set; }

        [JsonProperty("nvzName")]
        public string NvzName { get; set; }
    }
}
