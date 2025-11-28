using NMP.Portal.Models;

namespace NMP.Portal.ViewModels
{
    public class NutrientsLoadingLiveStockViewModel:NutrientsLoadingLiveStock
    {
        public string? LiveStockType { get; set; }
        public decimal? NitrogenStandard { get; set; }
    }
}
