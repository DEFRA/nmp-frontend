using Newtonsoft.Json;
namespace NMP.Core.ServiceResponses;
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

    [JsonProperty("nMaxLimitEngland")]
    public int? NMaxLimitEngland { get; set; }

    [JsonProperty("nMaxLimitWales")]
    public int? NMaxLimitWales { get; set; }

    [JsonProperty("sNSCategoryID")]
    public int? SNSCategoryID { get; set; }
}
