using NMP.Commons.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerFarmViewModel:MannerFarm
    {
        public DateTime? LastUpdatedDate { get; set; }
        public string? FieldName { get; set; }
        public string? ReferenceName { get; set; }
        public string? EncryptedId { get; set; }
    }
}
