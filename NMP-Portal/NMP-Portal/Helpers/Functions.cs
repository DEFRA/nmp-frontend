using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Portal.Enums;
using NMP.Portal.ViewModels;

namespace NMP.Portal.Helpers
{
    public static class Functions
    {
        public static readonly int[] SpecialManureTypes =
        {
            (int)Enums.ManureTypes.StrawMulch,
            (int)Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated,
            (int)Enums.ManureTypes.PaperCrumbleBiologicallyTreated
        };

        public static bool HasSpecialManure(List<int> manureHistory, int? manureTypeId)
        {
            return (manureHistory?.Intersect(SpecialManureTypes).Any() ?? false)
                || (manureTypeId.HasValue && SpecialManureTypes.Contains(manureTypeId.Value));
        }

        public static int ApplyYieldBonus(decimal? yield, decimal threshold, decimal step, int increment)
        {
            if (yield.HasValue && yield > threshold)
            {
                return (int)Math.Round(((yield.Value - threshold) / step) * increment);
            }
            return 0;
        }

        public static int ApplySoilTypeBonus(string soilType)
        {
            if (soilType.Equals("Shallow", StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }
            return 0;
        }

        public static int ApplyCropInfo1Bonus(int? cropInfo1)
        {
            if (cropInfo1 == (int)Enums.CropInfoOne.Milling)
            {
                return 40;
            }
            return 0;
        }

        public static int ApplyPotentialCutBonus(int potentialCut)
        {
            if (potentialCut >= (int)Enums.PotentialCut.Three)
            {
                return 40;
            }
            return 0;
        }

        public static bool IsManureBonusCrop(int cropTypeId)
        {
            int[] eligibleCrops =
            {
                (int)Enums.CropTypes.WinterWheat,
                (int)Enums.CropTypes.SpringWheat,
                (int)Enums.CropTypes.WinterBarley,
                (int)Enums.CropTypes.SpringBarley,
                (int)Enums.CropTypes.WinterOilseedRape,
                (int)Enums.CropTypes.SugarBeet,
                (int)Enums.CropTypes.PotatoVarietyGroup1,
                (int)Enums.CropTypes.PotatoVarietyGroup2,
                (int)Enums.CropTypes.PotatoVarietyGroup3,
                (int)Enums.CropTypes.PotatoVarietyGroup4,
                (int)Enums.CropTypes.ForageMaize,
                (int)Enums.CropTypes.WinterBeans,
                (int)Enums.CropTypes.SpringBeans,
                (int)Enums.CropTypes.Peas,
                (int)Enums.CropTypes.Asparagus,
                (int)Enums.CropTypes.Carrots,
                (int)Enums.CropTypes.Radish,
                (int)Enums.CropTypes.Swedes,
                (int)Enums.CropTypes.CelerySelfBlanching,
                (int)Enums.CropTypes.Courgettes,
                (int)Enums.CropTypes.DwarfBeans,
                (int)Enums.CropTypes.Lettuce,
                (int)Enums.CropTypes.BulbOnions,
                (int)Enums.CropTypes.SaladOnions,
                (int)Enums.CropTypes.Parsnips,
                (int)Enums.CropTypes.RunnerBeans,
                (int)Enums.CropTypes.Sweetcorn,
                (int)Enums.CropTypes.Turnips,
                (int)Enums.CropTypes.Beetroot,
                (int)Enums.CropTypes.BrusselSprouts,
                (int)Enums.CropTypes.Cabbage,
                (int)Enums.CropTypes.Calabrese,
                (int)Enums.CropTypes.Cauliflower,
                (int)Enums.CropTypes.Leeks,
                (int)Enums.CropTypes.Grass,
                (int)Enums.CropTypes.WholecropSpringBarley,
                (int)Enums.CropTypes.WholecropSpringWheat,
                (int)Enums.CropTypes.WholecropWinterBarley,
                (int)Enums.CropTypes.WholecropWinterWheat
            };

            return eligibleCrops.Contains(cropTypeId);
        }

        public static RedirectToActionResult RedirectToErrorHandler(int statusCode)
        {
            string errorController = "Error";
            string httpStatusCodeHandlerAction = "HttpStatusCodeHandler";
            return new RedirectToActionResult(
                httpStatusCodeHandlerAction,
                errorController,
                new { statusCode }
            );
        }

        public static List<SelectListItem> NormalizeDefoliationText(List<SelectListItem> items)
        {
            return items.Select(i =>
            {
                var parts = i.Text.Split('-');
                if (parts.Length == 2)
                {
                    var left = parts[0].Trim();
                    var right = Capitalize(parts[1]);
                    i.Text = $"{left} - {right}";
                }
                return i;
            }).ToList();
        }

        public static string Capitalize(string text)
        {
            text = text.Trim();
            return char.ToUpper(text[0]) + text[1..];
        }

        public static List<SelectListItem> GetCommonDefoliations(List<List<SelectListItem>> groups)
        {
            var commonText = groups
                .Select(l => l.Select(i => i.Text).ToList())
                .Aggregate((p, n) => p.Intersect(n).ToList());

            return groups
                .SelectMany(i => i)
                .Where(i => commonText.Contains(i.Text))
                .GroupBy(i => i.Text)
                .Select(g => g.First())
                .ToList();
        }

        public static string FormatDefoliationLabel(int num, string[] names)
        {
            if (num > 0 && num <= names.Length)
                return $"{Enum.GetName(typeof(PotentialCut), num)} - {names[num - 1]}";

            return num.ToString();
        }
    }
}
