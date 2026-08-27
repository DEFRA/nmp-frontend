using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep13ViewModel : MannerEstimationNWarningViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string ManureTypeName { get; set; } = string.Empty;
        public int FarmRB209CountryId { get; set; }
        public int? CropGroupId { get; set; }
        public int? TopSoilId { get; set; }
        public int? SubSoilId { get; set; }
        public DateTime? SowingDate { get; set; }
        public int? ManureGroupId { get; set; }

        

        public int? MannerEstimationApplicationsId { get; set; }
        public bool IsApplicationDateChange { get; set; } = false;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;

    }
}
