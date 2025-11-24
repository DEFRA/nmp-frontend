using NMP.Portal.Resources;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimitLogic
    {
        public int NMaxLimit(int nMaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId, int potentialCut , List<int> currentYearManureTypeIds,
            List<int> previousYearManureTypeIds, int? manureTypeId)
        {
            bool manureTypeCondition = false;
            if (currentYearManureTypeIds.Count > 0)
            {
                foreach (var Ids in currentYearManureTypeIds)
                {
                    if (Ids == (int)NMP.Portal.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                        Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                    {
                        manureTypeCondition = true;
                    }
                }
            }
            if (previousYearManureTypeIds.Count > 0)
            {
                foreach (var Ids in previousYearManureTypeIds)
                {
                    if (Ids == (int)NMP.Portal.Enums.ManureTypes.StrawMulch || Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                        Ids == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
                    {
                        manureTypeCondition = true;
                    }
                }
            }
            if (manureTypeId == (int)NMP.Portal.Enums.ManureTypes.StrawMulch || manureTypeId == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                        manureTypeId == (int)NMP.Portal.Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
            {
                manureTypeCondition = true;
            }
            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterWheat)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
                if (cropInfo1 == (int)NMP.Portal.Enums.CropInfoOne.Milling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 8.0m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropWinterWheat)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropWinterBarley)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringWheat)
            {
                if (cropInfo1 == (int)NMP.Portal.Enums.CropInfoOne.Milling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 7.0m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 7.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterBarley)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
                if (yield > 6.5m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 6.5m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringBarley)
            {
                if (yield > 5.5m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 5.5m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
            {
                if (yield > 3.5m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 3.5m) / 0.1m) * 6);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                if(potentialCut >= (int)NMP.Portal.Enums.PotentialCut.Three)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
            }

            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterWheat || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringWheat
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterBarley || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringBarley
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SugarBeet
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup1 || cropTypeId == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup2
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup3 || cropTypeId == (int)NMP.Portal.Enums.CropTypes.PotatoVarietyGroup4
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.ForageMaize || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterBeans
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringBeans || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Peas
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Asparagus || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Carrots
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Radish || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Swedes
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.CelerySelfBlanching || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Courgettes
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.DwarfBeans || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Lettuce
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.BulbOnions || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SaladOnions
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Parsnips || cropTypeId == (int)NMP.Portal.Enums.CropTypes.RunnerBeans
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Sweetcorn || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Turnips
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Beetroot || cropTypeId == (int)NMP.Portal.Enums.CropTypes.BrusselSprouts
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cabbage || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Calabrese
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Cauliflower || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Leeks
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropSpringBarley
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropSpringWheat
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropWinterBarley
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WholecropWinterWheat)
            {
                if (manureTypeCondition)
                {
                    nMaxLimit = nMaxLimit + 80;
                }
            }




            return nMaxLimit;

        }
    }
}
