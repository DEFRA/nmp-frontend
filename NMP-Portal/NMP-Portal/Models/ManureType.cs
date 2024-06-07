﻿namespace NMP.Portal.Models
{
    public class ManureType
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? ManureGroupId { get; set; }
        public int? CountryID { get; set; }
        public bool? IsLiquid { get; set; }
        public decimal? DryMatter { get; set; }
        public decimal? TotalN { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? Uric { get; set; }
        public decimal? NO3N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? MgO { get; set; }

    }
}
