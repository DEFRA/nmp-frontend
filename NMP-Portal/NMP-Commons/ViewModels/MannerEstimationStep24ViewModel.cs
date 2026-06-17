using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep24ViewModel
    {
        public string? ManureTypeName { get; set; }
        public ManureType? ManureType { get; set; }
        public bool? DefaultNutrientValue { get; set; }
        public bool? IsManureTypeLiquid { get; set; }
        public int? ApplicationMethodCount { get; set; }
    }
}
