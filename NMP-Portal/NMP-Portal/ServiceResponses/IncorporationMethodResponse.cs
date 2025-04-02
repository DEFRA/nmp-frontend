﻿using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class IncorporationMethodResponse
    {
        [JsonProperty("id")]
        public int? ID { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }
        
        [JsonProperty("sortOrder")]
        public int SortOrder {  get; set; }
    }
}
