using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using NMP.Commons.Enums;
using NMP.Commons.Resources;
using NMP.Commons.ServiceResponses;
namespace NMP.Commons.Helpers
{
    public static class Functions
    {
        private const string _1AugustTo31December = "1 August to 31 December";
        public static Error? ExtractError(this ILogger logger, ResponseWrapper? wrapper, Error? error)
        {
            if (wrapper != null && wrapper.Error != null)
            {
                error = wrapper?.Error?.ToObject<Error>();

                if (error != null)
                {
                    // Cast dynamic values to object to avoid dynamic dispatch for extension methods
                    logger.LogError(
                        "{Code} : {Message} : {Stack} : {Path}",
                        error.Code,
                        error.Message,
                        error.Stack,
                        error.Path);
                }
            }

            return error;
        }

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
        public static decimal ApplyYieldBonusScotland(decimal? yield, decimal threshold, decimal step, decimal increment)
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

        public static int ApplyPotentialCutBonus(int potentialCut, int? defoliationSequenceId)
        {
            //NMPT 2844
            // As per the discussion with RB209 team, DefoliationSequenceID will be used to identify the cuts for grass and if the sequence ID is 10,11,32,33,56,57,78 or 79 then it will be considered as 3 or more cuts and 40 points will be given for grass cut in that case.
            int[] threeOrMoreCutdefoliationSequenceIDs = { 10, 11, 32, 33, 56, 57, 78, 79 };
            if (defoliationSequenceId.HasValue && Array.Exists<int>(threeOrMoreCutdefoliationSequenceIDs, element => element == defoliationSequenceId))
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
                (int)CropTypes.MarketPickPeas,
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
                (int)CropTypes.WholecropWinterWheat,
                (int)CropTypes.BabyLeafLettuce
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

        public static Error HandleException(this ILogger logger, Exception ex, Error? error)
        {
            error ??= new Error();
            error.Message = ex.Message;
            logger.LogError(ex, ex.Message);
            return error;
        }

        public static Error HandleHttpRequestException(this ILogger logger, HttpRequestException hre, Error? error)
        {
            error ??= new Error();
            error.Message = Resource.MsgServiceNotAvailable;
            logger.LogError(hre, hre.Message);
            return error;
        }
        public static bool IsWinterOilseedRapeAutumn(int cropTypeId, int harvestYear, DateTime applicationDate)
        {
            bool isAutumn = false;
            if (cropTypeId == (int)NMP.Commons.Enums.CropTypes.WinterOilseedRape && applicationDate >= new DateTime(harvestYear - 1, 8, 1, 00, 00, 00, DateTimeKind.Unspecified) && applicationDate <= new DateTime(harvestYear - 1, 12, 31, 00, 00, 00, DateTimeKind.Unspecified)) //Winter Oilseed Rape - autumn nitrogen
            {
                isAutumn = true;
            }
            return isAutumn;
        }
        public static void BindCounter<T>(
    List<T> list,
    IDataProtector protector,
    Action<T, int, string> setValues)
        {
            if (list != null && list.Count > 0)
            {
                int counter = 1;
                list.ForEach(item =>
                {
                    var encrypted = protector.Protect($"{counter}");
                    setValues(item, counter++, encrypted);
                });
            }
        }
        private static int? GetHarvestYear(DateTime? sowingDate)
        {
            if (!sowingDate.HasValue)
                return null;

            return sowingDate.Value.Month >= 8
                ? sowingDate.Value.Year + 1
                : sowingDate.Value.Year;
        }
        public static string GetMannerClosedPeriod(
    bool isSandyShallowSoil,
    int fieldType,
    DateTime? sowingDate,
    int countryId,
    int? cropGroupId = null,
    int? cropTypeId = null,
    bool isPerennial = false)
        {
            int? harvestYear = GetHarvestYear(sowingDate);

            DateTime? september16 = harvestYear.HasValue
                ? new DateTime(harvestYear.Value - 1, 9, 16, 00, 00, 00, DateTimeKind.Unspecified)
                : null;

            DateTime? october1 = harvestYear.HasValue
                ? new DateTime(harvestYear.Value - 1, 10, 1, 00, 00, 00, DateTimeKind.Unspecified)
                : null;

            return countryId == 2
                ? GetScotlandClosedPeriod(
                    fieldType,
                    isSandyShallowSoil,
                    sowingDate,
                    cropGroupId,
                    cropTypeId,
                    september16,
                    october1)
                : GetEnglandWalesClosedPeriod(
                    fieldType,
                    isSandyShallowSoil,
                    sowingDate,
                    countryId,
                    harvestYear,
                    isPerennial,
                    september16);
        }
        
        private static string GetScotlandClosedPeriod(
    int fieldType,
    bool isSandyShallowSoil,
    DateTime? sowingDate,
    int? cropGroupId,
    int? cropTypeId,
    DateTime? september16,
    DateTime? october1)
        {
            if (fieldType == 2)
            {
                return isSandyShallowSoil
                    ? "1 September to 31 December"
                    : "15 October to 31 January";
            }

            if (fieldType == 1)
            {
                return GetScotlandArableClosedPeriod(
                    isSandyShallowSoil,
                    sowingDate,
                    cropGroupId,
                    cropTypeId,
                    september16,
                    october1);
            }

            return string.Empty;
        }
        private static string GetScotlandArableClosedPeriod(
    bool isSandyShallowSoil,
    DateTime? sowingDate,
    int? cropGroupId,
    int? cropTypeId,
    DateTime? september16,
    DateTime? october1)
        {
            if (!isSandyShallowSoil)
                return "1 October to 31 January";

            if (cropGroupId == 0)
            {
                return IsOnOrAfter(sowingDate, september16)
                    ? _1AugustTo31December
                    : "16 September to 31 December";
            }

            if (cropTypeId == 20)
            {
                return IsOnOrAfter(sowingDate, october1)
                    ? _1AugustTo31December
                    : "1 October to 31 December";
            }

            return _1AugustTo31December;
        }
        private static string GetEnglandWalesClosedPeriod(
    int fieldType,
    bool isSandyShallowSoil,
    DateTime? sowingDate,
    int countryId,
    int? harvestYear,
    bool isPerennial,
    DateTime? september16)
        {
            if (fieldType == 2)
            {
                return GetGrassClosedPeriod(
                    isSandyShallowSoil,
                    countryId);
            }

            if (fieldType == 1)
            {
                return GetEnglandWalesArableClosedPeriod(
                    isSandyShallowSoil,
                    sowingDate,
                    harvestYear,
                    isPerennial,
                    september16);
            }

            return string.Empty;
        }
        private static string GetGrassClosedPeriod(
    bool isSandyShallowSoil,
    int countryId)
        {
            if (isSandyShallowSoil)
                return "1 September to 31 December";

            return countryId == 3
                ? "15 October to 15 January"
                : "15 October to 31 January";
        }
        private static string GetEnglandWalesArableClosedPeriod(
    bool isSandyShallowSoil,
    DateTime? sowingDate,
    int? harvestYear,
    bool isPerennial,
    DateTime? september16)
        {
            bool isEstablishedPerennial =
                isPerennial &&
                sowingDate.HasValue &&
                harvestYear.HasValue &&
                sowingDate.Value.Year < harvestYear.Value;

            if (isEstablishedPerennial)
            {
                return isSandyShallowSoil
                    ? "16 September to 31 December"
                    : "1 October to 31 January";
            }

            if (!isSandyShallowSoil)
                return "1 October to 31 January";

            return IsOnOrAfter(sowingDate, september16)
                ? _1AugustTo31December
                : "16 September to 31 December";
        }
        private static bool IsOnOrAfter(
    DateTime? sowingDate,
    DateTime? comparisonDate)
        {
            return !sowingDate.HasValue ||
                   !comparisonDate.HasValue ||
                   sowingDate >= comparisonDate;
        }

        public static bool IsSlurry(int? manureTypeId)
        {
            if (!manureTypeId.HasValue) return false;

            var slurryTypes = new[]
            {
                (int)NMP.Commons.Enums.ManureTypes.PigSlurry,
                (int)NMP.Commons.Enums.ManureTypes.CattleSlurry,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryStrainerBox,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryWeepingWall,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedCattleSlurryMechanicalSeparator,
                (int)NMP.Commons.Enums.ManureTypes.SeparatedPigSlurryLiquidPortion
            };

            return slurryTypes.Contains(manureTypeId.Value);
        }
        public static bool IsPoultryManure(int? manureTypeId)
        {
            return manureTypeId == (int)NMP.Commons.Enums.ManureTypes.PoultryManure;
        }
    }
}