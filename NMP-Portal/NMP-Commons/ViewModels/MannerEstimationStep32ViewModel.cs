using NMP.Commons.Models;
using NMP.Commons.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Commons.ViewModels
{
    public class MannerEstimationStep32ViewModel
    {
        public int? ApplicationMethodId { get; set; }
        public DateTime? SoilDrainageEndDate { get; set; }
        public int? RainfallWithinSixHoursId { get; set; }
        public string? RainfallWithinSixHours { get; set; }
        public int? WindspeedId { get; set; }
        public string? Windspeed { get; set; }
        public int? MoistureTypeId { get; set; }
        public string? MoistureType { get; set; }
        public int? IncorporationMethodId { get; set; }
        public int? ApplicationRateMethod { get; set; }
        public int? TotalRainfall { get; set; }
        [Range(0, 9999,
    ErrorMessageResourceType = typeof(Resource),
    ErrorMessageResourceName = nameof(Resource.MsgEnterAValueBetween0And9999))]
        public int? AutumnCropNitrogenUptake { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? PostCode { get; set; }
        public int? CropTypeId { get; set; }
        public string? FieldName { get; set; }
        public string? CropTypeName { get; set; }
        public bool IsApplicationDateChange { get; set; } = false;
        public string EncryptedMannerEstimateId { get; set; } = string.Empty;
        public bool IsManureTypeChange { get; set; } = false;
        public bool IsComingForAddNewApplication { get; set; } = false;
    }
}
