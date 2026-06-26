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
        public string EncryptedId { get; set; } = string.Empty;
    }
}
