using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class LivestockTypeResponse
    {
        [JsonProperty("Id")]
        public int ID { get; set; }

        [JsonProperty("livestockGroupID")]
        public int LivestockGroupID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nByUnit")]
        public decimal? NByUnit { get; set; }

        [JsonProperty("nByUnitCalc")]
        public decimal? NByUnitCalc { get; set; }

        [JsonProperty("p2O5")]
        public decimal? P2O5 { get; set; }

        [JsonProperty("p2O5Calc")]
        public decimal? P2O5Calc { get; set; }

        [JsonProperty("occupancy")]
        public decimal? Occupancy { get; set; }

        [JsonProperty("orderBy")]
        public int? OrderBy { get; set; }
        
    }
}
