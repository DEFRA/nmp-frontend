using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class FarmManureTypeResponse
    {
        [JsonProperty("ID")]
        public int ID { get; set; }

        [JsonProperty("farmID")]
        public int FarmID { get; set; }

        [JsonProperty("manureTypeID")]
        public int ManureTypeID { get; set; }

        [JsonProperty("manureTypeName")]
        public string? ManureTypeName { get; set; }

        [JsonProperty("fieldTypeID")]
        public int FieldTypeID { get; set; }

        [JsonProperty("dryMatter")]
        public decimal DryMatter { get; set; }

        [JsonProperty("totalN")]
        public decimal TotalN { get; set; }

        [JsonProperty("nH4N")]
        public decimal NH4N { get; set; }

        [JsonProperty("uric")]
        public decimal Uric { get; set; }

        [JsonProperty("nO3N")]
        public decimal NO3N { get; set; }

        [JsonProperty("p2O5")]
        public decimal P2O5 { get; set; }

        [JsonProperty("k2O")]
        public decimal K2O { get; set; }

        [JsonProperty("sO3")]
        public decimal SO3 { get; set; }

        [JsonProperty("mgO")]
        public decimal MgO { get; set; }

        [JsonProperty("createdOn")]
        public DateTime? CreatedOn { get; set; }

        [JsonProperty("createdByID")]
        public int? CreatedByID { get; set; }

        [JsonProperty("modifiedOn")]
        public DateTime? ModifiedOn { get; set; }

        [JsonProperty("modifiedByID")]
        public int? ModifiedByID { get; set; }


    }
}
