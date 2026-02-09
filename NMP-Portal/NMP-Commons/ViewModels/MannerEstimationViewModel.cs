using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationViewModel
    {
        public MannerEstimationViewModel()
        {
            MannerEstimationStep1 = new MannerEstimationStep1();
            MannerEstimationStep2 = new MannerEstimationStep2();
            MannerEstimationStep3 = new MannerEstimationStep3();
        }
        public bool IsCheckAnswer { get; set; } = false;
        public string? Country { get; set; }
        public int? CropGroupID { get; set; }
        public string? CropGroup { get; set; }
        public bool? EnglishRues { get; set; }
        public string? CropType { get; set; }
        public string? SoilType { get; set; }
        public MannerEstimationStep1 MannerEstimationStep1 { get; set; }
        public MannerEstimationStep2 MannerEstimationStep2 { get; set; }
        public MannerEstimationStep3 MannerEstimationStep3 { get; set; }
    }
}
