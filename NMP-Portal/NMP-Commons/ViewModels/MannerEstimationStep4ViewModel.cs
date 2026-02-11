using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep4ViewModel
    {
        public string Postcode { get; set; } = string.Empty;
        public int AverageAnnualRainfall { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
    }
}
