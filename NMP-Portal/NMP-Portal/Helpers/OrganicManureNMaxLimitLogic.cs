using NMP.Commons.Enums;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimitLogic
    {       

        public int NMaxLimit(int nmaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId,
            int potentialCut, bool hasSpecialManure)
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
                        nmaxLimit += Functions.ApplyPotentialCutBonus(potentialCut);                   
                    break;
            }

            if (hasSpecialManure && Functions.IsManureBonusCrop(cropTypeId))
            {
                nmaxLimit += 80;
            }

            return nmaxLimit;
        }
    }
}
