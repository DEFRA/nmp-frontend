using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationDetailsViewModel:MannerEstimation
    {
        public string TopSoil { get; set; } = string.Empty;
        public string SubSoil { get; set; } = string.Empty;
        public string CropTypeName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public string EncryptedId { get; set; } = string.Empty;
    }
}
