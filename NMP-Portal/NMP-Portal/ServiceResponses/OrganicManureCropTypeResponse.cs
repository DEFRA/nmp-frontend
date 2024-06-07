using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class OrganicManureCropTypeResponse
    {
        [JsonProperty("CropTypeID")]
        public int CropTypeId { get; set; }

        [JsonProperty("CropTypeName")]
        public string CropType { get; set; }
    }
}
