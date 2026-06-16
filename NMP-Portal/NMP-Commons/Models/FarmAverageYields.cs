using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class FarmAverageYields
    {
        public int FarmID {  get; set; }
        public int HarvestYear { get; set; }
        public int CropTypeID { get; set; }
        public decimal? AverageYield { get; set; }
    }
}
