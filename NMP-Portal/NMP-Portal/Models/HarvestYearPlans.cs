﻿namespace NMP.Portal.Models
{
    public class HarvestYearPlans
    {
        public string CropTypeName { get; set; }
        public string CropVariety { get; set; }
        //public List<string> FieldNames { get; set; } = new List<string>();
       public Dictionary<string, string> FieldData { get; set; } = new Dictionary<string, string>();
    }
}
