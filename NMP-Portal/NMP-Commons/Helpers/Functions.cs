using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMP.Commons.Enums;
namespace NMP.Commons.Helpers
{
    public static class Functions
    {
        public static string ExtractFirstHalfPostcode(string postcode)
        {
            if (string.IsNullOrWhiteSpace(postcode))
            {
                return string.Empty;
            }

            postcode = postcode.Trim();
            int spaceIndex = postcode.IndexOf(' ');

            return spaceIndex > 0
                ? postcode[..spaceIndex]
                : postcode[..^3]; // remove last 3 characters
        }


        public static readonly int[] SpecialManureTypes =
        {
            (int)ManureTypes.StrawMulch,
            (int)ManureTypes.PaperCrumbleChemicallyPhysciallyTreated,
            (int)ManureTypes.PaperCrumbleBiologicallyTreated
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
            if (cropInfo1 == (int)CropInfoOne.Milling)
            {
                return 40;
            }
            return 0;
        }

        public static int ApplyPotentialCutBonus(int potentialCut)
        {
            if (potentialCut >= (int)PotentialCut.Three)
            {
                return 40;
            }
            return 0;
        }

        public static bool IsManureBonusCrop(int cropTypeId)
        {
            int[] eligibleCrops =
            {
                (int)CropTypes.WinterWheat,
                (int)CropTypes.SpringWheat,
                (int)CropTypes.WinterBarley,
                (int)CropTypes.SpringBarley,
                (int)CropTypes.WinterOilseedRape,
                (int)CropTypes.SugarBeet,
                (int)CropTypes.PotatoVarietyGroup1,
                (int)CropTypes.PotatoVarietyGroup2,
                (int)CropTypes.PotatoVarietyGroup3,
                (int)CropTypes.PotatoVarietyGroup4,
                (int)CropTypes.ForageMaize,
                (int)CropTypes.WinterBeans,
                (int)CropTypes.SpringBeans,
                (int)CropTypes.Peas,
                (int)CropTypes.Asparagus,
                (int)CropTypes.Carrots,
                (int)CropTypes.Radish,
                (int)CropTypes.Swedes,
                (int)CropTypes.CelerySelfBlanching,
                (int)CropTypes.Courgettes,
                (int)CropTypes.DwarfBeans,
                (int)CropTypes.Lettuce,
                (int)CropTypes.BulbOnions,
                (int)CropTypes.SaladOnions,
                (int)CropTypes.Parsnips,
                (int)CropTypes.RunnerBeans,
                (int)CropTypes.Sweetcorn,
                (int)CropTypes.Turnips,
                (int)CropTypes.Beetroot,
                (int)CropTypes.BrusselSprouts,
                (int)CropTypes.Cabbage,
                (int)CropTypes.Calabrese,
                (int)CropTypes.Cauliflower,
                (int)CropTypes.Leeks,
                (int)CropTypes.Grass,
                (int)CropTypes.WholecropSpringBarley,
                (int)CropTypes.WholecropSpringWheat,
                (int)CropTypes.WholecropWinterBarley,
                (int)CropTypes.WholecropWinterWheat
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

        public static string FormatPart(string? part) =>
            string.IsNullOrWhiteSpace(part) ? string.Empty : $"{part}, ";
    }
}
