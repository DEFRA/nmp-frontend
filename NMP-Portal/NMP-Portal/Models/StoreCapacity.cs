using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

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
        public int? SolidManureTypeID { get; set; }  //not in view model

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Length { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Width { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? Depth { get; set; }

        public decimal? Circumference { get; set; }
        public decimal? Diameter { get; set; }
        public int? BankSlopeAngleID { get; set; }
        public bool? IsCovered { get; set; }
        public decimal? CapacityVolume { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.lblEnterAPositiveValue))]
        public decimal? CapacityWeight { get; set; }
        public decimal? SurfaceArea { get; set; }
    }
}
