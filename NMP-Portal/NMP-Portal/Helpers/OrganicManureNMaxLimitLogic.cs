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

        public int NMaxLimitScotland(int nmaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId, int potentialCut, bool hasSpecialManure, int? defoliationSequenceId, int? rainfall) 
        {
            nmaxLimit += GetCropSpecificAdjustment(cropTypeId, yield, cropInfo1);

            nmaxLimit += GetRainfallAdjustment(cropTypeId, rainfall);

            nmaxLimit += GetGrassAdjustment(cropTypeId, potentialCut, defoliationSequenceId);

            if (hasSpecialManure && Functions.IsManureBonusCrop(cropTypeId))
            {
                nmaxLimit += 80;
            }

            return nmaxLimit;
        }

        private int GetCropSpecificAdjustment(int cropTypeId, decimal? yield, int? cropInfo1)
        {
            int adjustment = 0;

            switch (cropTypeId)
            {
                case (int)CropTypes.WinterWheat:
                    if (cropInfo1 == (int)CropInfoOne.Milling)
                    {
                        adjustment += 40;
                    }

                    adjustment += Functions.ApplyYieldBonus(yield, 8.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.WinterBarley:
                    adjustment += Functions.ApplyYieldBonus(yield, 6.5m, 0.1m, 2);
                    break;

                case (int)CropTypes.SpringWheat:
                    adjustment += Functions.ApplyYieldBonus(yield, 7.0m, 0.1m, 2);
                    break;

                case (int)CropTypes.SpringBarley:
                    adjustment += Functions.ApplyYieldBonus(yield, 5.5m, 0.1m, 2);
                    break;

                case (int)CropTypes.WinterOilseedRape:
                    adjustment += Functions.ApplyYieldBonus(yield, 3.5m, 0.1m, 6);
                    break;
            }

            return adjustment;
        }
        private int GetRainfallAdjustment(int cropTypeId, int? rainfall)
        {
            if (!rainfall.HasValue)
                return 0;

            int adjustment = 0;

            // Example logic (replace with actual Excel rules)
            if (rainfall > 700 && rainfall <= 1000)
            {
                adjustment += 20;
            }
            else if (rainfall > 1000)
            {
                adjustment += 40;
            }

            return adjustment;
        }
        private int GetGrassAdjustment(int cropTypeId, int potentialCut, int? defoliationSequenceId)
        {
            if (cropTypeId == (int)CropTypes.Grass)
            {
                return Functions.ApplyPotentialCutBonus(potentialCut, defoliationSequenceId);
            }

            return 0;
        }
#pragma warning restore S107
    }
}
