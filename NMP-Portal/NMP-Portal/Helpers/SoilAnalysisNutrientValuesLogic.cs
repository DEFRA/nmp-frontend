using NMP.Commons.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Application;
using NMP.Businesses;
using NMP.Commons.ServiceResponses;
using NMP.Commons.ViewModels;

namespace NMP.Portal.Helpers
{
    public class SoilAnalysisNutrientValuesLogic
    {
        
        public  List<SelectListItem>? BindViewBagForScotlandNutrient(List<SoilNutrientStatusResponse> statusList,
            List<NutrientResponseWrapper> nutrients,
            string nutrientName,
            int defaultId)
        {
            var nutrientId = nutrients
               .FirstOrDefault(n => n.nutrient.Equals(nutrientName))?.nutrientId
               ?? defaultId;

            return statusList
                .Where(x => x.nutrientId == nutrientId)
                .Select(x => new SelectListItem
                {
                    Text = x.indexText,
                    Value = x.index
                })
                .ToList();
        }

        public static SoilAnalysisViewModel BindSoilNutrientValueType(SoilAnalysisViewModel model)
        {
            if ((model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Miligram))
            {
                model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
            }            
            else if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.SoilNutrientValueType == (int)NMP.Commons.Enums.SoilNutrientValueType.Status))
            {
                model.SoilNutrientValueTypeName = Resource.lblAsAStatus;
            }
            else
            {
                model.SoilNutrientValueTypeName = Resource.lblIndexValues;
            }
            return model;
        }
    }
}
