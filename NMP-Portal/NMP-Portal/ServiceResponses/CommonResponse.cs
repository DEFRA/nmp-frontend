﻿using Newtonsoft.Json;

namespace NMP.Portal.ServiceResponses
{
    public class CommonResponse
    {
        [JsonProperty("ID")]
        public int Id { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public int? Value { get; set; }
        [JsonProperty("sortOrder")]
        public int? SortOrder { get; set; }
    }
}
