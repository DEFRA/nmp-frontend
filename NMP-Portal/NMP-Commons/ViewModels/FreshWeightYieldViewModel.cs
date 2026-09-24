using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class FreshWeightYieldViewModel
    {
        public int Position { get; set; }
        public int? Yield { get; set; }
        public string? DefoliationSequenceName { get; set; }
        public string? DefoliationName { get; set; }
    }
}
