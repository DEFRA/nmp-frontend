namespace NMP.Portal.Models
{
    public class NutrientsLoadingManures
    {
        public int? FarmId { get; set; }
        public string? ManureLookupType { get; set; }
        public int ManureTypeId { get; set; }
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
        public string? Comments { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        //public int ModifiedByID { get; set; }
    }
}
