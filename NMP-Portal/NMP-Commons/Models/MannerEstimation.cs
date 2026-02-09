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
        public string? FarmName { get; set; }

        public int? CountryID { get; set; }
        public string? Postcode { get; set; }
        public int? AverageAnuualRainfall { get; set; }

        public string? FieldName { get; set; }
        public bool? IsWithinNVZ { get; set; }
        public int? NVZProgrammeID        { get; set; }
        public int? SoilTypeID { get; set; }
        public int? CropTypeID { get; set; }
        public bool? IsEarlySown { get; set; }
        public string? FieldComments { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }

    }
}
