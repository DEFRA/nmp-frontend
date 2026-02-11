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
            MannerEstimationStep1 = new MannerEstimationStep1ViewModel();
            MannerEstimationStep2 = new MannerEstimationStep2ViewModel();
            MannerEstimationStep3 = new MannerEstimationStep3ViewModel();
            MannerEstimationStep4 = new MannerEstimationStep4ViewModel();
            MannerEstimationStep5 = new MannerEstimationStep5ViewModel();
            MannerEstimationStep6 = new MannerEstimationStep6ViewModel();
        }
        public bool IsCheckAnswer { get; set; } = false;
        public string? Country { get; set; }
        public int? CropGroupID { get; set; }
        public string? CropGroup { get; set; }
        public bool? EnglishRues { get; set; }
        public string? CropType { get; set; }
        public string? SoilType { get; set; }
        public MannerEstimationStep1ViewModel MannerEstimationStep1 { get; set; }
        public MannerEstimationStep2ViewModel MannerEstimationStep2 { get; set; }
        public MannerEstimationStep3ViewModel MannerEstimationStep3 { get; set; }
        public MannerEstimationStep4ViewModel MannerEstimationStep4 { get; set; }
        public MannerEstimationStep5ViewModel MannerEstimationStep5 { get; set; }
        public MannerEstimationStep6ViewModel MannerEstimationStep6 { get; set; }
    }
}
