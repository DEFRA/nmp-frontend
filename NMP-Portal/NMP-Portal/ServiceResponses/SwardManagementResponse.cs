using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class SwardManagementResponse
    {
        [JsonProperty("swardManagementId")]
        public int SwardManagementId { get; set; }

        [JsonProperty("swardManagement")]
        public string SwardManagement { get; set; }
    }
}
