using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationApplicationDetailsViewModel:MannerEstimationApplication
    {
        public string ManureType { get; set; } = string.Empty;
        public string Windspeed{ get; set; } = string.Empty;
        public string RainType { get; set; } = string.Empty;
        public string MoistureType { get; set; } = string.Empty;
        public string ApplicationMethod { get; set; } = string.Empty;
        public string IncorporationMethod { get; set; } = string.Empty;
        public string IncorporationDelay { get; set; } = string.Empty;
    }
}
