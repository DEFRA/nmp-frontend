using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class MannerEstimation
    {
        public int? ID { get; set; }

        public string? Name { get; set; }
        public Guid? OrganisationID { get; set; }
        public string? FarmName { get; set; }

        public int? CountryID { get; set; }
        public string? Postcode { get; set; }
        public int? AverageAnuualRainfall { get; set; }
        public bool? RegisteredOrganicProducer { get; set; }

        public string? FieldName { get; set; }
        public bool? IsWithinNVZ { get; set; }
        public int? TopSoilID { get; set; }
        public int? SubSoilID { get; set; }
        public int? CropTypeID { get; set; }  
        public int? MannerCropTypeID { get; set; }
        public DateTime? SowingDate { get; set; }
        public int NitrogenProductId { get; set; } = 0;
        public int PhosphateProductId { get; set; } = 0;
        public int PotashProductId { get; set; } = 0;


        public string NitrogenProductName { get; set; } = string.Empty;
        public string PhosphateProductName { get; set; } = string.Empty;
        public string PotashProductName { get; set; } = string.Empty;

        public int NitrogenProductPrice { get; set; } = 0;
        public int PhosphateProductPrice { get; set; } = 0;
        public int PotashProductPrice { get; set; } = 0;

        public decimal NitrogenPrice { get; set; } = 0;
        public decimal PhosphatePrice { get; set; } = 0;
        public decimal PotashPrice { get; set; } = 0;

        public bool CalculateBasedOnNutrientPrice { get; set; } = false;    

        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }

    }
}
