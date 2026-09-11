using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ServiceResponses
{
    public class SecondCropLinkingResponse
    {
        [JsonProperty("firstCropID")]
        public int FirstCropID { get; set; }

        [JsonProperty("secondCropID")]
        public int SecondCropID { get; set; }

        [JsonProperty("rB209CountryID")]
        public int RB209CountryID { get; set; }

    }
}
