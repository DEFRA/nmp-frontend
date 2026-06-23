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
            MannerEstimationStep11 = new MannerEstimationStep11ViewModel();
            MannerEstimationStep12 = new MannerEstimationStep12ViewModel();
            MannerEstimationStep13 = new MannerEstimationStep13ViewModel();
            MannerEstimationStep14 = new MannerEstimationStep14ViewModel();
            MannerEstimationStep15 = new MannerEstimationStep15ViewModel();
            MannerEstimationStep16 = new MannerEstimationStep16ViewModel();
            MannerEstimationStep17 = new MannerEstimationStep17ViewModel();
            MannerEstimationStep18 = new MannerEstimationStep18ViewModel();
            MannerEstimationStep19 = new MannerEstimationStep19ViewModel();
            MannerEstimationStep20 = new MannerEstimationStep20ViewModel();
            MannerEstimationStep23 = new MannerEstimationStep23ViewModel();
            MannerEstimationStep24 = new MannerEstimationStep24ViewModel();
            MannerEstimationStep25 = new MannerEstimationStep25ViewModel();
            MannerEstimationStep26 = new MannerEstimationStep26ViewModel();
            MannerEstimationStep27 = new MannerEstimationStep27ViewModel();
            MannerEstimationStep28 = new MannerEstimationStep28ViewModel();
            MannerEstimationStep21 = new MannerEstimationStep21ViewModel();
            MannerEstimationStep22 = new MannerEstimationStep22ViewModel();
            MannerEstimationStep29 = new MannerEstimationStep29ViewModel();
            MannerEstimationStep30 = new MannerEstimationStep30ViewModel();
            MannerEstimationStep31 = new MannerEstimationStep31ViewModel();
            MannerEstimationStep32 = new MannerEstimationStep32ViewModel();
        }
        public bool IsCheckAnswer { get; set; } = false;
        public bool? IsCopyEstimate { get; set; }
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
        public MannerEstimationStep11ViewModel MannerEstimationStep11 { get; set; }
        public MannerEstimationStep12ViewModel MannerEstimationStep12 { get; set; }
        public MannerEstimationStep13ViewModel MannerEstimationStep13 { get; set; }
        public MannerEstimationStep14ViewModel MannerEstimationStep14 { get; set; }
        public MannerEstimationStep15ViewModel MannerEstimationStep15 { get; set; }
        public MannerEstimationStep16ViewModel MannerEstimationStep16 { get; set; }
        public MannerEstimationStep17ViewModel MannerEstimationStep17 { get; set; }
        public MannerEstimationStep18ViewModel MannerEstimationStep18 { get; set; }
        public MannerEstimationStep19ViewModel MannerEstimationStep19 { get; set; }
        public MannerEstimationStep20ViewModel MannerEstimationStep20 { get; set; }
        public MannerEstimationStep23ViewModel MannerEstimationStep23 { get; set; }
        public MannerEstimationStep24ViewModel MannerEstimationStep24 { get; set; }
        public MannerEstimationStep25ViewModel MannerEstimationStep25 { get; set; }
        public MannerEstimationStep26ViewModel MannerEstimationStep26 { get; set; }
        public MannerEstimationStep27ViewModel MannerEstimationStep27 { get; set; }
        public MannerEstimationStep28ViewModel MannerEstimationStep28 { get; set; }
        public MannerEstimationStep21ViewModel MannerEstimationStep21 { get; set; }
        public MannerEstimationStep22ViewModel MannerEstimationStep22 { get; set; }
        public MannerEstimationStep29ViewModel MannerEstimationStep29 { get; set; }
        public MannerEstimationStep30ViewModel MannerEstimationStep30 { get; set; }
        public MannerEstimationStep31ViewModel MannerEstimationStep31 { get; set; }
        public MannerEstimationStep32ViewModel MannerEstimationStep32 { get; set; }
    }
}

