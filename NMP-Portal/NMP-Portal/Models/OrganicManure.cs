﻿namespace NMP.Portal.Models
{
    public class OrganicManure
    {
        public int ID { get; set; }
        public int ManagementPeriodID { get; set; }
        public int ManureTypeID { get; set; }
        public DateTime AppDate { get; set; }
        public bool Confirm { get; set; }
        public decimal N { get; set; }
        public decimal P2O5 { get; set; }
        public decimal K2O { get; set; }
        public decimal MgO { get; set; }
        public decimal SO3 { get; set; }
        public decimal AvailableN { get; set; }
        public int AppRate { get; set; }
        public decimal DryMatterPercent { get; set; }
        public decimal UricAcid { get; set; }
        public DateTime EndOfDrain { get; set; }
        public int Rainfall { get; set; }
        public decimal AreaSpread { get; set; }
        public decimal ManureQuantity { get; set; }
        public int ApplicationMethodID { get; set; }
        public int IncroporationMethodID { get; set; }
        public int IncroporationDelayID { get; set; }
        public decimal NH4N { get; set; }
        public decimal NO3N { get; set; }
        public decimal AvailableP2O5 { get; set; }
        public decimal AvailableK2O { get; set; }
        public int WindspeedID { get; set; }
        public int RainfallWithin6HoursID { get; set; }
        public int MoistureID { get; set; }
    }
}
