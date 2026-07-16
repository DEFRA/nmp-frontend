using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep11ViewModel
    {
        public int? ManureGroupId { get; set; }
        public string ManureGroupName { get; set; } = string.Empty;
        public bool IsComingForAddNewApplication { get; set; } = false;
        public string EncryptedMannerEstimationApplicationId { get; set; } = string.Empty;
        public string EncryptedMannerEstimationId { get; set; } = string.Empty;
        public int CropTypeId { get; set; }
        public int? CountryId { get; set; }
        public bool? IsFarmOrganic { get; set; }
        public bool? IsWithinNVZ { get; set; }
    }
}
