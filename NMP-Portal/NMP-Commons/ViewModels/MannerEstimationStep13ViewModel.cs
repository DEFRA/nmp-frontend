using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep13ViewModel
    {
        public DateTime? ApplicationDate { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ManureTypeName { get; set; } = string.Empty;
        public bool IsCheckAnswer { get; set; } = false;
        public int FarmRB209CountryId { get; set; }
        public int? CropTypeId { get; set; }
        public int? CropGroupId { get; set; }
        public int? TopSoilId { get; set; }
        public int? SubSoilId { get; set; }
        public DateTime? SowingDate { get; set; }

        public bool IsWarningMsgNeedToShow { get; set; } = false;
        public bool IsClosedPeriodWarning { get; set; } = false;

    }
}
