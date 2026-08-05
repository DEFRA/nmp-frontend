using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.Models
{
    public class MannerFarm
    {
        public int? ID { get; set; }

        public string? Name { get; set; }
        public Guid? OrganisationID { get; set; }

        public int? CountryID { get; set; }
        public string? Postcode { get; set; }
        public int? AverageAnuualRainfall { get; set; }
        public bool? RegisteredOrganicProducer { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedByID { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedByID { get; set; }

    }
}
