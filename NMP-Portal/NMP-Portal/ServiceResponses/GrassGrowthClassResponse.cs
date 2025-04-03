﻿using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class GrassGrowthClassResponse
    {
        [JsonProperty("grassGrowthClassId")]
        public int GrassGrowthClassId { get; set; }

        [JsonProperty("grassGrowthClassName")]
        public string GrassGrowthClassName { get; set; }

        [JsonProperty("fieldId")]
        public int FieldId { get; set; }

    }
}
