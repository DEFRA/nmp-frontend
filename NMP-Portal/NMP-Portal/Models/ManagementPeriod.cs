namespace NMP.Portal.Models
{
    public class ManagementPeriod
    {
        public int? ID { get; set; }
        public int? CropID { get; set; }
        public int? Defoliation { get; set; }
        public int? Utilisation1ID { get; set; }
        public int? Utilisation2ID { get; set; }
        //public decimal? Yield { get; set; }
        public DateTime? PloughedDown { get; set; }
        public int? CreatedByID { get; set; }
        public int? ModifiedByID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? PreviousID { get; set; }
    }
}
