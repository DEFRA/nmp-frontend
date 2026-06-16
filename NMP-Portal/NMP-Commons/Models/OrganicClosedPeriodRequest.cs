using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class OrganicClosedPeriodRequest
    {
        public int SoilTypeId { get; set; }
        public int FieldType { get; set; }
        public int HarvestYear { get; set; }
        public string? SowingDate { get; set; }
        public int CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public int? CropTypeId { get; set; }
        public bool IsPerennial { get; set; }
    }
}
