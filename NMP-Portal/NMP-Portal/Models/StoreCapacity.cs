namespace NMP.Portal.Models
{
    public class StoreCapacity
    {
        public int? ID { get; set; }
        public int? FarmID { get; set; }
        public int? Year { get; set; }
        public string? StoreName { get; set; }
        public int? MaterialStateID { get; set; }
        public int? StorageTypeID { get; set; }
        public int? SolidManureTypeID { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Depth { get; set; }
        public decimal? Circumference { get; set; }
        public decimal? Diameter { get; set; }
        public int? BankSlopeAngleID { get; set; }
        public bool? IsCovered { get; set; }
        public decimal? CapacityVolume { get; set; }
        public decimal? CapacityWeight { get; set; }
        public decimal? SurfaceArea { get; set; }
    }
}
