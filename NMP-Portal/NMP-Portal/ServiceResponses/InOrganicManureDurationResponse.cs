﻿using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class InOrganicManureDurationResponse
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ApplicationDate")]
        public int ApplicationDate { get; set; }
        [JsonProperty("ApplicationMonth")]
        public int ApplicationMonth { get; set; }
    }
}
