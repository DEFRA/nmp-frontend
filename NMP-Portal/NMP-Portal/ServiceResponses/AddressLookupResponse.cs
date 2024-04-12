using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace NMP.Portal.ServiceResponses
{
    public class AddressLookupResponse
    {
        [JsonProperty("addressLine")]
        public string AddressLine { get; set; }

        [JsonProperty("buildingNumber")]
        public string BuildingNumber { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("locality")]
        public string Locality { get; set; }

        [JsonProperty("town")]
        public string Town { get; set; }

        [JsonProperty("administrativeArea")]
        public string AdministrativeArea { get; set; }

        [JsonProperty("historicCounty")]
        public string HistoricCounty { get; set; }

        [JsonProperty("ceremonialCounty")]
        public string CeremonialCounty { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("xCoordinate")]
        public int XCoordinate { get; set; }

        [JsonProperty("yCoordinate")]
        public int YCoordinate { get; set; }

        [JsonProperty("uprn")]
        public string Uprn { get; set; }

        [JsonProperty("match")]
        public string Match { get; set; }

        [JsonProperty("matchDescription")]
        public string MatchDescription { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }
    }
}
