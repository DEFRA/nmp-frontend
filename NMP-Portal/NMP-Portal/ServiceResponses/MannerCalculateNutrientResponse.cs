using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class MannerCalculateNutrientResponse
    {
        [JsonProperty("fieldID")]
        public int FieldID { get; set; }
        [JsonProperty("fieldName")]
        public string FieldName { get; set; } = string.Empty;
        [JsonProperty("totalN")]
        public int TotalN { get; set; }
        [JsonProperty("mineralisedN")]
        public int MineralisedN { get; set; }
        [JsonProperty("nitrateNLoss")]
        public int NitrateNLoss { get; set; }
        [JsonProperty("ammoniaNLoss")]
        public int AmmoniaNLoss { get; set; }
        [JsonProperty("denitrifiedNLoss")]
        public int DenitrifiedNLoss { get; set; }
        [JsonProperty("currentCropAvailableN")]
        public int CurrentCropAvailableN { get; set; }
        [JsonProperty("nextGrassNCropCurrentYear")]
        public int NextGrassNCropCurrentYear { get; set; }
        [JsonProperty("followingCropYear2AvailableN")]
        public int FollowingCropYear2AvailableN { get; set; }
        [JsonProperty("nitrogenEfficiencePercentage")]
        public int NitrogenEfficiencePercentage { get; set; }
        [JsonProperty("totalP2O5")]
        public int TotalP2O5 { get; set; }
        [JsonProperty("cropAvailableP2O5")]
        public int CropAvailableP2O5 { get; set; }
        [JsonProperty("totalK2O")]
        public int TotalK2O { get; set; }
        [JsonProperty("cropAvailableK2O")]
        public int CropAvailableK2O { get; set; }
        [JsonProperty("totalSO3")]
        public int TotalSO3 { get; set; }
        [JsonProperty("cropAvailableSO3")]
        public int? CropAvailableSO3 { get; set; }
        [JsonProperty("totalMgO")]
        public int TotalMgO { get; set; }
    }
}
