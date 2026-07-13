using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep12ViewModel
    {
        public int ManureGroupId { get; set; }
        public string ManureGroupName { get; set; } = string.Empty;
        public int? ManureTypeId { get; set; }
        public string ManureTypeName { get; set; } = string.Empty;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public int FarmRB209CountryId { get; set; }
        public bool IsManureTypeChange { get; set; }=false;
        public bool IsComingForAddNewApplication { get; set; } = false;
    }
}
