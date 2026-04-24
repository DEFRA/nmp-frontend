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
        
        public List<SelectListItem>? BindViewBagForScotlandNutrient(List<SoilNutrientStatusResponse> statusList,
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
                    Value = MapIndexText(x.indexText)
                })
                .ToList();
        }

        public static string MapIndexText(string indexText) => indexText switch
        {
            "Very low (1)" => "VL",
            "Low (2)" => "L",
            "Moderate minus (3)" => "-M",
            "Moderate plus (4)" => "+M",
            "High (5)" => "H",
            "Very high (6)" => "VH",
            _ => indexText
        };
        public  string MapValueToText(string value) => value switch
        {
            "VL" => "Very low (1)",
            "L" => "Low (2)",
            "-M" => "Moderate minus (3)",
            "+M" => "Moderate plus (4)",
            "H" => "High (5)",
            "VH" => "Very high (6)",
            _ => value
        };
        public  SoilAnalysisViewModel BindSoilNutrientValueType(SoilAnalysisViewModel model)
        {

            if (model.FarmRB209CountryID == (int)NMP.Commons.Enums.RB209Country.Scotland && (model.MagnesiumStatus != null || model.PotassiumStatus != null || model.PhosphorusStatus != null))
            {
                model.SoilNutrientValueTypeName = Resource.lblAsAStatus;
            }
            else if (model.Phosphorus != null ||
             model.Potassium != null || model.Magnesium != null)
            {
                model.SoilNutrientValueTypeName = Resource.lblMiligramValues;
            }
            else
            {
                model.SoilNutrientValueTypeName = Resource.lblIndexValues;
            }
            return model;
        }
    }
}
