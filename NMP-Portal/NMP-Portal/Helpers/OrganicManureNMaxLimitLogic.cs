using NMP.Portal.Enums;
using NMP.Portal.Resources;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimitLogic
    {
        private static readonly int[] SpecialManureTypes =
        {
            (int)Enums.ManureTypes.StrawMulch,
            (int)Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated,
            (int)Enums.ManureTypes.PaperCrumbleBiologicallyTreated
        };

        private bool HasSpecialManure(List<int> manureHistory, int? manureTypeId)
        {
            return (manureHistory?.Intersect(SpecialManureTypes).Any() ?? false)
                || (manureTypeId.HasValue && SpecialManureTypes.Contains(manureTypeId.Value));
        }

        private int ApplyYieldBonus(decimal? yield, decimal threshold, decimal step, int increment)
        {
            if (yield.HasValue && yield > threshold)
            {
                return (int)Math.Round(((yield.Value - threshold) / step) * increment);
            }
            return 0;
        }

        private int ApplySoilTypeBonus(string soilType)
        {
            if (soilType == Resource.lblShallow)
            {
                return 20;
            }
            return 0;
        }

        private int ApplyCropInfo1Bonus(int? cropInfo1)
        {
            if (cropInfo1 == (int)Enums.CropInfoOne.Milling)
            {
                return 40;
            }
            return 0;
        }

        private int ApplyPotentialCutBonus(int potentialCut)
        {
            if (potentialCut >= (int)Enums.PotentialCut.Three)
            {
                return 40;
            }
            return 0;
        }

        private bool IsManureBonusCrop(int cropTypeId)
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

        public int NMaxLimit(int nmaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId,
            int potentialCut, List<int> currentYearManure, List<int> previousYearManure, int? manureTypeId)
        {
            bool hasSpecialManure = HasSpecialManure(currentYearManure, manureTypeId)
                                 || HasSpecialManure(previousYearManure, manureTypeId);

            switch (cropTypeId)
            {
                case (int)Enums.CropTypes.WinterWheat:
                    nmaxLimit += ApplySoilTypeBonus(soilType);
                    nmaxLimit += ApplyCropInfo1Bonus(cropInfo1);
                    nmaxLimit += ApplyYieldBonus(yield, 8.0m, 0.1m, 2);
                    break;

                case (int)Enums.CropTypes.WholecropWinterWheat:
                case (int)Enums.CropTypes.WholecropWinterBarley:
                    nmaxLimit += ApplySoilTypeBonus(soilType);
                    break;

                case (int)Enums.CropTypes.SpringWheat:
                    nmaxLimit += ApplyCropInfo1Bonus(cropInfo1);
                    nmaxLimit += ApplyYieldBonus(yield, 7.0m, 0.1m, 2);
                    break;

                case (int)Enums.CropTypes.WinterBarley:
                    nmaxLimit += ApplySoilTypeBonus(soilType);
                    nmaxLimit += ApplyYieldBonus(yield, 6.5m, 0.1m, 2);
                    break;

                case (int)Enums.CropTypes.SpringBarley:
                    nmaxLimit += ApplyYieldBonus(yield, 5.5m, 0.1m, 2);
                    break;

                case (int)Enums.CropTypes.WinterOilseedRape:
                    nmaxLimit += ApplyYieldBonus(yield, 3.5m, 0.1m, 6);
                    break;

                case (int)Enums.CropTypes.Grass:                    
                        nmaxLimit += ApplyPotentialCutBonus(potentialCut);                   
                    break;
            }

            if (hasSpecialManure && IsManureBonusCrop(cropTypeId))
            {
                nmaxLimit += 80;
            }

            return nmaxLimit;
        }
    }
}
