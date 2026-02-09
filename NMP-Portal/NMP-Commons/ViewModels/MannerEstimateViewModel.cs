using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimateViewModel : MannerEstimate
    {
        public List<MannerEstimationApplication>? MannerEstimationApplications { get; set; }
        public bool IsCheckAnswer { get; set; } = false; 
        public string? Country { get; set; }
        public int? CropGroupID { get; set; }
        public string? CropGroup { get; set; }
        public bool? EnglishRues { get; set; }
        public string? CropType { get; set; }
        public string? SoilType { get; set; }
    }
}
