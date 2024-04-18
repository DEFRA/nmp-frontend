using Microsoft.AspNetCore.Http.HttpResults;
using NMP.Portal.Resources;
using System.ComponentModel.DataAnnotations;

namespace NMP.Portal.Models
{
    public class Field
    {
        public int ID { get; set; }
        public int FarmID { get; set; }
        public int SoilTypeID { get; set; }
        public string?  NVZProgrammeID { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblFieldName))]
        public string Name { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblLandParcelID))]
        public string? LPIDNumber { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblNationalGridReference))]
        public string? NationalGridReference { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblTotalFieldArea))]
        public decimal TotalArea { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblCroppedArea))]
        public decimal? CroppedArea { get; set; }

        [Display(ResourceType = typeof(Resource), Name = nameof(Resource.lblManureNonSpreadingArea))]
        public decimal? ManureNonSpreadingArea { get; set; }
        public Boolean SoilReleasingClay { get; set; }
        public Boolean IsWithinNVZ { get; set; }
        public Boolean IsAbove300SeaLevel { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedByID { get; set; }
        public DateTime ModifiedOn { get; set; }
        public int ModifiedByID { get; set; }

    }
}
