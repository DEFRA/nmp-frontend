namespace NMP.Portal.Models
{
    public class FertiliserManure
    {
        public int Id { get; set; }
        public int ManagementPeriodID { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int? ApplicationRate { get; set; }
        public bool Confirm { get; set; }
        public decimal? N { get; set; }
        public decimal? P2O5 { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public decimal? Na2O { get; set; }
        public decimal? NFertAnalysisPercent { get; set; }
        public decimal? P2O5FertAnalysisPercent { get; set; }
        public decimal? K2OFertAnalysisPercent { get; set; }
        public decimal? MgOFertAnalysisPercent { get; set; }
        public decimal? SO3FertAnalysisPercent { get; set; }
        public decimal? Na2OFertAnalysisPercent { get; set; }
        public decimal? Lime { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }
    }
}
