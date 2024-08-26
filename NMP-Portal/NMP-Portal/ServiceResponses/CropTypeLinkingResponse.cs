using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class CropTypeLinkingResponse
    {
        [JsonProperty("MannerCropTypeID")]
        public int MannerCropTypeID { get; set; }

        [JsonProperty("DefaultYield")]
        public decimal? DefaultYield { get; set; }

        [JsonProperty("IsPerennial")]
        public bool IsPerennial { get; set; }

        [JsonProperty("NMaxLimit")]
        public int? NMaxLimit { get; set; }
    }
}
