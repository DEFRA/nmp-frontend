using NMP.Commons.Enums;
using NMP.Commons.Helpers;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimitLogic
    {
#pragma warning disable S107
        public int NMaxLimit(int nmaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId,
            int potentialCut, bool hasSpecialManure, int? defoliationSequenceId)
        {

            switch (cropTypeId)
            {
                case (int)CropTypes.WinterWheat:
                    nmaxLimit += Functions.ApplySoilTypeBonus(soilType);
                    nmaxLimit += Functions.ApplyCropInfo1Bonus(cropInfo1);
                    nmaxLimit += Functions.ApplyYieldBonus(yield, 8.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.WholecropWinterWheat:
                case (int)CropTypes.WholecropWinterBarley:
                    nmaxLimit += Functions.ApplySoilTypeBonus(soilType);
                    break;

                case (int)CropTypes.SpringWheat:
                    nmaxLimit += Functions.ApplyCropInfo1Bonus(cropInfo1);
                    nmaxLimit += Functions.ApplyYieldBonus(yield, 7.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.WinterBarley:
                    nmaxLimit += Functions.ApplySoilTypeBonus(soilType);
                    nmaxLimit += Functions.ApplyYieldBonus(yield, 6.5m, 0.1m, 2);
                    break;

                case (int)CropTypes.SpringBarley:
                    nmaxLimit += Functions.ApplyYieldBonus(yield, 5.5m, 0.1m, 2);
                    break;

                case (int)CropTypes.WinterOilseedRape:
                    nmaxLimit += Functions.ApplyYieldBonus(yield, 3.5m, 0.1m, 6);
                    break;

                case (int)CropTypes.Grass:
                    nmaxLimit += Functions.ApplyPotentialCutBonus(potentialCut, defoliationSequenceId);
                    break;
            }

            if (hasSpecialManure && Functions.IsManureBonusCrop(cropTypeId))
            {
                nmaxLimit += 80;
            }

            return nmaxLimit;
        }

        public  decimal NMaxLimitScotland(decimal nmaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId, int potentialCut, int? defoliationSequenceId, int? rainfall, int residueGroup)
        {
            nmaxLimit += GetCropSpecificAdjustmentScotland(cropTypeId, yield, cropInfo1);

            int[] eligibleCropsForRainfallAdjustment =
            {
                (int)CropTypes.WinterWheat,
                (int)CropTypes.WholecropWinterWheat,

                (int)CropTypes.WinterBarley,
                (int)CropTypes.WholecropWinterBarley,

                (int)CropTypes.SpringWheat,
                (int)CropTypes.WholecropSpringWheat,
                (int)CropTypes.WheatSpringUndersown,

                (int)CropTypes.SpringBarley,
                (int)CropTypes.WholecropSpringBarley,
                (int)CropTypes.BarleySpringUndersown,

                (int)CropTypes.WinterOats,
                (int)CropTypes.WholecropWinterOats,

                (int)CropTypes.SpringOats,
                (int)CropTypes.WholecropSpringOats,
                (int)CropTypes.OatsSpringUndersown,

                (int)CropTypes.PotatoVarietyGroup1,
                (int)CropTypes.PotatoVarietyGroup2,
                (int)CropTypes.PotatoVarietyGroup3,
                (int)CropTypes.PotatoVarietyGroup4,

                (int)CropTypes.WinterOilseedRape,

                (int)CropTypes.SpringRye,
                (int)CropTypes.ForageSpringRye,

                (int)CropTypes.SpringTriticale,
                (int)CropTypes.ForageSpringTriticale,
                (int)CropTypes.TriticaleSpringUndersown,


            };

            if (eligibleCropsForRainfallAdjustment.Contains(cropTypeId) && rainfall != null)
            {
                nmaxLimit += GetRainfallAdjustment(residueGroup, rainfall, soilType);
            }

            return nmaxLimit;
        }

        private static decimal GetCropSpecificAdjustmentScotland(int cropTypeId, decimal? yield, int? cropInfo1)
        {
            decimal adjustment = 0;

            switch (cropTypeId)
            {
                case (int)CropTypes.WinterWheat:
                case (int)CropTypes.WholecropWinterWheat:
                    if (cropInfo1 == (int)CropInfoOne.Milling)
                    {
                        adjustment += 40;
                    }

                    adjustment += Functions.ApplyYieldBonusScotland(yield, 8.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.WinterBarley:
                case (int)CropTypes.WholecropWinterBarley:
                    adjustment += Functions.ApplyYieldBonusScotland(yield, 6.5m, 0.1m, 1.5m);
                    break;

                case (int)CropTypes.SpringWheat:
                case (int)CropTypes.WholecropSpringWheat:
                case (int)CropTypes.WheatSpringUndersown:
                    if (cropInfo1 == (int)CropInfoOne.Milling)
                    {
                        adjustment += 40;
                    }
                    adjustment += Functions.ApplyYieldBonusScotland(yield, 7.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.SpringBarley:
                case (int)CropTypes.WholecropSpringBarley:
                case (int)CropTypes.BarleySpringUndersown:
                    if (cropInfo1 == (int)CropInfoOne.HighNGrainDistilling)
                    {
                        adjustment += 15;
                    }
                    adjustment += Functions.ApplyYieldBonusScotland(yield, 5.5m, 0.1m, 1.5m);
                    break;

                case (int)CropTypes.WinterOats:
                case (int)CropTypes.WholecropWinterOats:

                    adjustment += Functions.ApplyYieldBonusScotland(yield, 6.0m, 0.1m, 1.5m);
                    break;

                case (int)CropTypes.SpringOats:
                case (int)CropTypes.WholecropSpringOats:
                case (int)CropTypes.OatsSpringUndersown:

                    adjustment += Functions.ApplyYieldBonusScotland(yield, 5.0m, 0.1m, 1.5m);
                    break;

                case (int)CropTypes.WinterOilseedRape:
                    if (yield > 4.0m)
                        adjustment += 30;
                    break;
            }

            return adjustment;
        }


        private static int GetRainfallAdjustment(int nResGroup, decimal? winterRainfall, string soilType)
        {

            bool isSandySoil = soilType == "Sands"
                               || soilType == "Shallow"
                               || soilType == "Sandy loams";

            bool isOtherSoil = soilType == "Other mineral"
                               || soilType == "Humose"
                               || soilType == "Peaty";

            int adjustment = 0;

            if (isSandySoil && winterRainfall >= 450 && nResGroup != 1)
            {
                if (nResGroup == 2)
                    adjustment = 10;
                else if (nResGroup >= 3 && nResGroup <= 6)
                    adjustment = 20;
            }
            else if (isOtherSoil && winterRainfall >= 450 && nResGroup >= 2 && nResGroup <= 6)
            {
                adjustment = 10;
            }

            return adjustment;
        }

#pragma warning restore S107
    }
}