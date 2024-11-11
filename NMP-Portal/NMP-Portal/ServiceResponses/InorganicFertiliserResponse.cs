using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class InorganicFertiliserResponse
    {
        [JsonProperty("ID")]
        public int ID { get; set; }
        [JsonProperty("ApplicationDate")]
        public DateTime? ApplicationDate { get; set; }
        [JsonProperty("Field")]
        public string? Field { get; set; }
        [JsonProperty("Crop")]
        public string? Crop { get; set; }
        [JsonProperty("N")]
        public decimal? N { get; set; }
        [JsonProperty("P2O5")]
        public decimal? P2O5 { get; set; }
        [JsonProperty("K2O")]
        public decimal? K2O { get; set; }
        [JsonProperty("MgO")]
        public decimal? MgO { get; set; }
        [JsonProperty("SO3")]
        public decimal? SO3 { get; set; }
        [JsonProperty("DryMatterPercent")]
        public decimal? DryMatterPercent { get; set; }
        [JsonProperty("UricAcid")]
        public decimal? UricAcid { get; set; }
    }
}
