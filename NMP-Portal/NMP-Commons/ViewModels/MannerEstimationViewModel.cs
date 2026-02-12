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
            MannerEstimationStep7 = new MannerEstimationStep7ViewModel();
            MannerEstimationStep8 = new MannerEstimationStep8ViewModel();
            MannerEstimationStep9 = new MannerEstimationStep9ViewModel();
            MannerEstimationStep10 = new MannerEstimationStep10ViewModel();
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
        public MannerEstimationStep7ViewModel MannerEstimationStep7 { get; set; }
        public MannerEstimationStep8ViewModel MannerEstimationStep8 { get; set; }
        public MannerEstimationStep9ViewModel MannerEstimationStep9 { get; set; }
        public MannerEstimationStep10ViewModel MannerEstimationStep10 { get; set; }
    }
}
