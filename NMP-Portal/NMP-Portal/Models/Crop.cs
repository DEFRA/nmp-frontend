using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class Crop
    {
        //public int Id { get; set; }
        //public int FieldId { get; set; }
        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblWhichHarvestWouldYouLikeToPlanFor))]
        public int? Year { get; set; }
        public int? CropTypeID { get; set; }
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
        public int? PreviousID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }
    }
}
