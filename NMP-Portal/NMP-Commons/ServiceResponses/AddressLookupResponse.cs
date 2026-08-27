using Newtonsoft.Json;

namespace NMP.Commons.ServiceResponses
{
    public class AddressLookupResponse
    {
        [JsonProperty("addressLine")]
        public string AddressLine { get; set; } = string.Empty;

        [JsonProperty("buildingNumber")]
        public string BuildingNumber { get; set; } = string.Empty;

        [JsonProperty("street")]
        public string Street { get; set; } = string.Empty;

        [JsonProperty("locality")]
        public string Locality { get; set; } = string.Empty;

        [JsonProperty("town")]
        public string Town { get; set; } = string.Empty;

        [JsonProperty("administrativeArea")]
        public string AdministrativeArea { get; set; } = string.Empty;

        [JsonProperty("historicCounty")]
        public string HistoricCounty { get; set; } = string.Empty;

        [JsonProperty("ceremonialCounty")]
        public string CeremonialCounty { get; set; } = string.Empty;

        [JsonProperty("postcode")]
        public string Postcode { get; set; } = string.Empty;

        [JsonProperty("country")]
        public string Country { get; set; } = string.Empty;

        [JsonProperty("xCoordinate")]
        public int XCoordinate { get; set; }

        [JsonProperty("yCoordinate")]
        public int YCoordinate { get; set; }

        [JsonProperty("uprn")]
        public string Uprn { get; set; } = string.Empty;

        [JsonProperty("match")]
        public string Match { get; set; } = string.Empty;

        [JsonProperty("matchDescription")]
        public string MatchDescription { get; set; } = string.Empty;

        [JsonProperty("language")]
        public string Language { get; set; } = string.Empty;

        [JsonProperty("subBuildingName")]
        public string SubBuildingName { get; set; } = string.Empty;

        [JsonProperty("BuildingName")]
        public string BuildingName { get; set; } = string.Empty;
    }
}
