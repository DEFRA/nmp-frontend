using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Commons.Models;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
using System.Xml.Linq;

namespace NMP.Portal.Helpers
{
    public class CommonHelpers
    {
        public  string ShorthandDefoliationSequence(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                return "";
            }

            Dictionary<string, int> defoliationSequence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (string item in data)
            {
                string name = item.Trim().ToLower();
                if (defoliationSequence.ContainsKey(name))
                {
                    defoliationSequence[name]++;
                }
                else
                {
                    defoliationSequence[name] = 1;
                }
            }

            List<string> result = FormatDefoliationSequenceEntries(defoliationSequence);

            return string.Join(", ", result);
        }

        private static List<string> FormatDefoliationSequenceEntries(Dictionary<string, int> defoliationSequence)
        {
            List<string> result = new List<string>();

            foreach (var entry in defoliationSequence)
            {
                string word = entry.Key;

                if (entry.Value > 1)
                {
                    if (word.EndsWith('s') || word.EndsWith('x') || word.EndsWith('z') ||
                        word.EndsWith("sh") || word.EndsWith("ch"))
                    {
                        word += "es";
                    }
                    else
                    {
                        word += "s";
                    }
                }


                word = char.ToUpper(word[0]) + word.Substring(1);
                result.Add($"{entry.Value} {word}");
            }

            return result;
        }
        public Recommendation FetchRecommendation(Recommendation recommendation)
        {
            var rec = new Recommendation
            {
                ID = recommendation.ID,
                ManagementPeriodID = recommendation.ManagementPeriodID,
                CropN = recommendation.CropN,
                CropP2O5 = recommendation.CropP2O5,
                CropK2O = recommendation.CropK2O,
                CropSO3 = recommendation.CropSO3,
                CropMgO = recommendation.CropMgO,
                CropNa2O = recommendation.CropNa2O,
                CropLime = (recommendation.PreviousAppliedLime != null && recommendation.PreviousAppliedLime > 0) ? recommendation.PreviousAppliedLime : recommendation.CropLime,
                ManureN = recommendation.ManureN,
                ManureP2O5 = recommendation.ManureP2O5,
                ManureK2O = recommendation.ManureK2O,
                ManureSO3 = recommendation.ManureSO3,
                ManureMgO = recommendation.ManureMgO,
                ManureLime = recommendation.ManureLime,
                ManureNa2O = recommendation.ManureNa2O,
                FertilizerN = recommendation.FertilizerN,
                FertilizerP2O5 = recommendation.FertilizerP2O5,
                FertilizerK2O = recommendation.FertilizerK2O,
                FertilizerSO3 = recommendation.FertilizerSO3,
                FertilizerMgO = recommendation.FertilizerMgO,
                FertilizerLime = recommendation.FertilizerLime,
                FertilizerNa2O = recommendation.FertilizerNa2O,
                SNSIndex = recommendation.SNSIndex,
                SIndex = recommendation.SIndex,
                LimeIndex = recommendation.PH,
                KIndex = recommendation.KIndex != null ? (recommendation.KIndex == Resource.lblMinusTwo ? Resource.lblTwoMinus : (recommendation.KIndex == Resource.lblPlusTwo ? Resource.lblTwoPlus : recommendation.KIndex)) : null,
                MgIndex = recommendation.MgIndex,
                PIndex = recommendation.PIndex,
                NaIndex = recommendation.NaIndex,
                NIndex = recommendation.NIndex,
                CreatedOn = recommendation.CreatedOn,
                ModifiedOn = recommendation.ModifiedOn,
                PBalance = recommendation.PBalance,
                SBalance = recommendation.SBalance,
                KBalance = recommendation.KBalance,
                MgBalance = recommendation.MgBalance,
                LimeBalance = recommendation.LimeBalance,
                NaBalance = recommendation.NaBalance,
                NBalance = recommendation.NBalance,
                FertiliserAppliedN = recommendation.FertiliserAppliedN,
                FertiliserAppliedP2O5 = recommendation.FertiliserAppliedP2O5,
                FertiliserAppliedK2O = recommendation.FertiliserAppliedK2O,
                FertiliserAppliedMgO = recommendation.FertiliserAppliedMgO,
                FertiliserAppliedSO3 = recommendation.FertiliserAppliedSO3,
                FertiliserAppliedNa2O = recommendation.FertiliserAppliedNa2O,
                FertiliserAppliedLime = recommendation.FertiliserAppliedLime,
                FertiliserAppliedNH4N = recommendation.FertiliserAppliedNH4N,
                FertiliserAppliedNO3N = recommendation.FertiliserAppliedNO3N,

            };
            return rec;
        }
    }
}
