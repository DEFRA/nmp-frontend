using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class CropTypeLinkingResponse
    {
        [JsonProperty("cropTypeId")]
        public int CropTypeId { get; set; }

        [JsonProperty("mannerCropTypeID")]
        public int MannerCropTypeID { get; set; }

        [JsonProperty("defaultYield")]
        public decimal? DefaultYield { get; set; }

        [JsonProperty("isPerennial")]
        public bool? IsPerennial { get; set; }

        [JsonProperty("nMaxLimit")]
        public int? NMaxLimit { get; set; }
    }
}
