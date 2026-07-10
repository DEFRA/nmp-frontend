using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerFieldAndCropDetailsViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string TopSoil { get; set; } = string.Empty;
        public string SubSoil { get; set; } = string.Empty;
        public string CropType { get; set; } = string.Empty;
        public bool IsWithinNVZ { get; set; }

    }
}
