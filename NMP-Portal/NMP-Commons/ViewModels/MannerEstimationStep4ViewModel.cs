using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep4ViewModel
    {
        public string Postcode { get; set; } = string.Empty;
        public int AverageAnnualRainfall { get; set; }
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsPostCodeChange { get; set; } = false;
    }
}