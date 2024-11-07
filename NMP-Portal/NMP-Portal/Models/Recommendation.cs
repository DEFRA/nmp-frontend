using System;

namespace NMP.Portal.Models
{
    public class Recommendation
    {
        public int? ID { get; set; }
        public int? ManagementPeriodID { get; set; }
        public decimal? CropN { get; set; }
        public decimal? CropP2O5 { get; set; }
        public decimal? CropK2O { get; set; }
        public decimal? CropMgO { get; set; }
        public decimal? CropSO3 { get; set; }
        public decimal? CropNa2O { get; set; }
        public decimal? CropLime { get; set; }
        public decimal? ManureN { get; set; }
        public decimal? ManureP2O5 { get; set; }
        public decimal? ManureK2O { get; set; }
        public decimal? ManureMgO { get; set; }
        public decimal? ManureSO3 { get; set; }
        public decimal? ManureNa2O { get; set; }
        public decimal? ManureLime { get; set; }
        public decimal? FertilizerN { get; set; }
        public decimal? FertilizerP2O5 { get; set; }
        public decimal? FertilizerK2O { get; set; }
        public decimal? FertilizerMgO { get; set; }
        public decimal? FertilizerSO3 { get; set; }
        public decimal? FertilizerNa2O { get; set; }
        public decimal? FertilizerLime { get; set; }
        public string? PH { get; set; }
        public string? SNSIndex { get; set; }
        public string? PIndex { get; set; }
        public string? KIndex { get; set; }
        public string? MgIndex { get; set; }
        public string? SIndex { get; set; }
        public string? NaIndex { get; set; }
        public string? Comments { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
