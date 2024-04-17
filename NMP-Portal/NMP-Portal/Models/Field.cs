using Microsoft.AspNetCore.Http.HttpResults;

namespace NMP.Portal.Models
{
    public class Field
    {
        public int ID { get; set; }
        public int FarmID { get; set; }
        public int SoilTypeID { get; set; }
        public string  NVZProgrammeID { get; set; }
        public string Name { get; set; }
        public string LPIDNumber { get; set; }
        public string NationalGridReference { get; set; }
        public decimal TotalArea { get; set; }
        public decimal CroppedArea { get; set; }
        public decimal ManureNonSpreadingArea { get; set; }
        public Boolean SoilReleasingClay { get; set; }
        public Boolean IsWithinNVZ { get; set; }
        public Boolean IsAbove300SeaLevel { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedByID { get; set; }
        public DateTime ModifiedOn { get; set; }
        public int ModifiedByID { get; set; }

    }
}
