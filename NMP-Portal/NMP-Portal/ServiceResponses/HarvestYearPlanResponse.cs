using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class HarvestYearPlanResponse
    {
        [JsonProperty("CropID")]
        public int CropID { get; set; }
        [JsonProperty("cropTypeId")]
        public int CropTypeID { get; set; }

        [JsonProperty("fieldID")]
        public int FieldID { get; set; }

        [JsonProperty("fieldName")]
        public string FieldName { get; set; }

        [JsonProperty("cropVariety")]
        public string CropVariety { get; set; }

        [JsonProperty("otherCropName")]
        public string OtherCropName { get; set; }
        [JsonProperty("CropInfo1")]
        public string CropInfo1 { get; set; }
        [JsonProperty("Yield")]
        public string Yield { get; set; }

        [JsonProperty("CropGroupName")]
        public string? CropGroupName { get; set; }

        [JsonProperty("lastModifiedOn")]
        public DateTime LastModifiedOn { get; set; }

        [JsonProperty("cropTypeName")]
        public string CropTypeName { get; set; }
        [JsonProperty("year")]
        public int Year { get; set; }
        [JsonProperty("sowingdate")]
        public DateTime? Sowingdate { get; set; }
        [JsonProperty("cropOrder")]
        public int? CropOrder { get; set; }
        [JsonProperty("TotalOrganicManures")]
        public int OrganicManuresCount { get; set; }

        [JsonProperty("totalFertiliserManures")]
        public int TotalFertiliserManures { get; set; }

    }
}
