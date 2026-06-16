using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep23ViewModel
    {
        public int? CropGroupId { get; set; }
        public int? CountryId { get; set; }
        public int? ManureGroupId { get; set; }
        public int? ManureTypeId { get; set; }
        public int? ApplicationMethodCount { get; set; }
        public int? ApplicationMethodId { get; set; }
        public string? ManureTypeName { get; set; }
        public string? OtherMaterialName { get; set; }
    }
}
