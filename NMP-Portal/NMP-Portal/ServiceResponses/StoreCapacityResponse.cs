using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.ServiceResponses
{
    public class StoreCapacityResponse
    {

        [JsonProperty("iD")]
        public int? ID { get; set; }

        [JsonProperty("farmID")]
        public int? FarmID { get; set; }

        [JsonProperty("year")]
        public int? Year { get; set; }

        [JsonProperty("storeName")]
        public string? StoreName { get; set; }

        [JsonProperty("materialStateID")]
        public int? MaterialStateID { get; set; }

        [JsonProperty("storageTypeID")]
        public int? StorageTypeID { get; set; }

        [JsonProperty("solidManureTypeID")]
        public int? SolidManureTypeID { get; set; }

        [JsonProperty("length")]
        public decimal? Length { get; set; }

        [JsonProperty("width")]
        public decimal? Width { get; set; }

        [JsonProperty("depth")]
        public decimal? Depth { get; set; }

        [JsonProperty("circumference")]
        public decimal? Circumference { get; set; }

        [JsonProperty("diameter")]
        public decimal? Diameter { get; set; }

        [JsonProperty("bankSlopeAngleID")]
        public int? BankSlopeAngleID { get; set; }

        [JsonProperty("isCovered")]
        public bool? IsCovered { get; set; }

        [JsonProperty("capacityVolume")]
        public decimal? CapacityVolume { get; set; }

        [JsonProperty("capacityWeight")]
        public decimal? CapacityWeight { get; set; }

        [JsonProperty("surfaceArea")]
        public decimal? SurfaceArea { get; set; }

        [JsonProperty("createdOn")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("createdByID")]
        public int? CreatedByID { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("modifiedByID")]
        public int? ModifiedByID { get; set; }

        [JsonProperty("storageTypeName")]
        public string? StorageTypeName { get; set; }

        [JsonProperty("solidManureTypeName")]
        public string? SolidManureTypeName { get; set; }
    }
}
