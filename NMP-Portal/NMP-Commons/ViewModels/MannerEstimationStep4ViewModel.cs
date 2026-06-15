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
        public string? Postcode { get; set; }
        [Range(1, 3000,
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterRainfallBetween1And3000))]        
        public int AverageAnnualRainfall { get; set; }
        public bool IsCheckAnswer { get; set; } = false;
    }
}
