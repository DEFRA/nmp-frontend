namespace NMP.Portal.Models
{
    public class FertiliserManure
    {
        public int? ID { get; set; }
        public int ManagementPeriodID { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int? ApplicationRate { get; set; }
        public bool Confirm { get; set; }
        public int? N { get; set; }
        public int? P2O5 { get; set; }
        public int? K2O { get; set; }
        public int? MgO { get; set; }
        public int? SO3 { get; set; }
        public int? Na2O { get; set; }
        public decimal? NFertAnalysisPercent { get; set; }
        public decimal? P2O5FertAnalysisPercent { get; set; }
        public decimal? K2OFertAnalysisPercent { get; set; }
        public decimal? MgOFertAnalysisPercent { get; set; }
        public decimal? SO3FertAnalysisPercent { get; set; }
        public decimal? Na2OFertAnalysisPercent { get; set; }
        public decimal? Lime { get; set; }
        public int? NH4N { get; set; }
        public int? NO3N { get; set; }
        public int? Defoliation { get; set; }
        public string? DefoliationName { get; set; }
        public string? FieldName { get; set; }
        public int? FieldID { get; set; }
        public string? EncryptedCounter { get; set; }
    }
}
