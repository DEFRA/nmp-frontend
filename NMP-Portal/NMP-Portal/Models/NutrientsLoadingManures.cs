namespace NMP.Portal.Models
{
    public class NutrientsLoadingManures
    {
        public int? ID { get; set; }
        public int? FarmID { get; set; }
        public string? ManureLookupType { get; set; }
        public int ManureTypeID { get; set; }
        public string? ManureType { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? NContent { get; set; }
        public decimal? NTotal { get; set; }
        public decimal? PContent { get; set; }
        public decimal? PTotal { get; set; }
        public DateTime? ManureDate { get; set; }
        public string? FarmName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Address4 { get; set; }
        public string? PostCode { get; set; }
        public decimal? NH4N { get; set; }
        public decimal? NO3N { get; set; }
        public decimal? DryMatterPercent { get; set; }
        public decimal? UricAcid { get; set; }
        public decimal? K2O { get; set; }
        public decimal? MgO { get; set; }
        public decimal? SO3 { get; set; }
        public string? Comments { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        //public int ModifiedByID { get; set; }
    }
}
