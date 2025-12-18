using Newtonsoft.Json;
namespace NMP.Commons.ServiceResponses;
public class CropDetailResponse
{
    [JsonProperty("CropId")]
    public int? CropId { get; set; }
    [JsonProperty("CropTypeID")]
    public int CropTypeID { get; set; }
    [JsonProperty("CropTypeName")]
    public string? CropTypeName { get; set; }
    [JsonProperty("CropGroupID")]
    public int? CropGroupID { get; set; }
    [JsonProperty("CropGroupName")]
    public string? CropGroupName { get; set; }
    [JsonProperty("FieldID")]
    public int FieldID { get; set; }
    [JsonProperty("FieldName")]
    public string? FieldName { get; set; }
    [JsonProperty("CropVariety")]
    public string? CropVariety { get; set; }
    [JsonProperty("OtherCropName")]
    public string? OtherCropName { get; set; }
    [JsonProperty("CropInfo1")]
    public int? CropInfo1 { get; set; }
    [JsonProperty("Yield")]
    public decimal? Yield { get; set; }
    [JsonProperty("LastModifiedOn")]
    public DateTime? LastModifiedOn { get; set; }
    [JsonProperty("PlantingDate")]
    public DateTime? PlantingDate { get; set; }
    [JsonProperty("Management")]
    public string? Management { get; set; } 
}
