using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class FreshWeightYieldForFieldViewModel
    {
        public int FieldId { get; set; }
        public string? FieldName { get; set; } = string.Empty;
        public bool? IsFreshWeightYieldsDefault { get; set; }

        public List<FreshWeightYieldViewModel>? FreshWeightYields { get; set; }
    }
}
