﻿namespace NMP.Portal.Models
{
    public class Crop
    {
        public int Id { get; set; }
        public int FieldId { get; set; }
        public int Year { get; set; }
        public int? CropTypeId { get; set; }
        public string? Variety { get; set; }
        public int? CropInfo1 { get; set; }
        public int? CropInfo2 { get; set; }
        public DateTime? SowingDate { get; set; }
        public decimal? Yield { get; set; }
        public bool Confirm { get; set; }
        public int? PreviousGrass { get; set; }
        public int? GrassHistory { get; set; }
        public string? Comments { get; set; }
        public int? Establishment { get; set; }
        public int? LivestockType { get; set; }
        public decimal? MilkYield { get; set; }
        public decimal? ConcentrateUse { get; set; }
        public decimal? StockingRate { get; set; }
        public int? DefoliationSequence { get; set; }
        public int? GrazingIntensity { get; set; }
        public int? PreviousId { get; set; }
    }
}
