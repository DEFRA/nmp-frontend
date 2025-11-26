using NMP.Portal.Resources;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimitLogic
    {
        public int NMaxLimit(int nMaxLimit, decimal? yield, string soilType, int? cropInfo1, int cropTypeId, int potentialCut, List<int> currentYearManureTypeIds,
            List<int> previousYearManureTypeIds, int? manureTypeId)
        {
            bool manureTypeCondition = false;

            if(currentYearManureTypeIds.Any(id =>
                        id == (int)Enums.ManureTypes.StrawMulch ||
                        id == (int)Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                        id == (int)Enums.ManureTypes.PaperCrumbleBiologicallyTreated))
            {
                manureTypeCondition = true;
            }

            if (previousYearManureTypeIds.Any(id =>
                        id == (int)Enums.ManureTypes.StrawMulch ||
                        id == (int)Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                        id == (int)Enums.ManureTypes.PaperCrumbleBiologicallyTreated))
            {
                manureTypeCondition = true;
            }

            if (manureTypeId == (int)Enums.ManureTypes.StrawMulch || 
                manureTypeId == (int)Enums.ManureTypes.PaperCrumbleChemicallyPhysciallyTreated ||
                manureTypeId == (int)Enums.ManureTypes.PaperCrumbleBiologicallyTreated)
            {
                manureTypeCondition = true;
            }

            if (cropTypeId == (int)Enums.CropTypes.WinterWheat)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
                if (cropInfo1 == (int)Enums.CropInfoOne.Milling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 8.0m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)Enums.CropTypes.WholecropWinterWheat)
            {
                if (soilType == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
            }
            else if (cropTypeId == (int)Enums.CropTypes.WholecropWinterBarley && soilType == Resource.lblShallow)
            {
               nMaxLimit = nMaxLimit + 20;               
            }
            else if (cropTypeId == (int)Enums.CropTypes.SpringWheat)
            {
                if (cropInfo1 == (int)Enums.CropInfoOne.Milling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 7.0m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 7.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)Enums.CropTypes.WinterBarley)
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
            else if (cropTypeId == (int)Enums.CropTypes.SpringBarley && yield > 5.5m)
            {                
                nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 5.5m) / 0.1m) * 2);                
            }
            else if (cropTypeId == (int)Enums.CropTypes.WinterOilseedRape && yield > 3.5m)
            {                
               nMaxLimit = nMaxLimit + (int)Math.Round(((yield.Value - 3.5m) / 0.1m) * 6);               
            }
            else if (cropTypeId == (int)Enums.CropTypes.Grass && potentialCut >= (int)Enums.PotentialCut.Three)
            {                
                nMaxLimit = nMaxLimit + 40;                
            }

            if (manureTypeCondition && (cropTypeId == (int)Enums.CropTypes.WinterWheat || 
                cropTypeId == (int)Enums.CropTypes.SpringWheat || 
                cropTypeId == (int)Enums.CropTypes.WinterBarley || 
                cropTypeId == (int)Enums.CropTypes.SpringBarley || 
                cropTypeId == (int)Enums.CropTypes.WinterOilseedRape || 
                cropTypeId == (int)Enums.CropTypes.SugarBeet || 
                cropTypeId == (int)Enums.CropTypes.PotatoVarietyGroup1 || 
                cropTypeId == (int)Enums.CropTypes.PotatoVarietyGroup2 || 
                cropTypeId == (int)Enums.CropTypes.PotatoVarietyGroup3 || 
                cropTypeId == (int)Enums.CropTypes.PotatoVarietyGroup4 || 
                cropTypeId == (int)Enums.CropTypes.ForageMaize || 
                cropTypeId == (int)Enums.CropTypes.WinterBeans || 
                cropTypeId == (int)Enums.CropTypes.SpringBeans || 
                cropTypeId == (int)Enums.CropTypes.Peas || 
                cropTypeId == (int)Enums.CropTypes.Asparagus || 
                cropTypeId == (int)Enums.CropTypes.Carrots || 
                cropTypeId == (int)Enums.CropTypes.Radish || 
                cropTypeId == (int)Enums.CropTypes.Swedes || 
                cropTypeId == (int)Enums.CropTypes.CelerySelfBlanching || 
                cropTypeId == (int)Enums.CropTypes.Courgettes || 
                cropTypeId == (int)Enums.CropTypes.DwarfBeans || 
                cropTypeId == (int)Enums.CropTypes.Lettuce || 
                cropTypeId == (int)Enums.CropTypes.BulbOnions || 
                cropTypeId == (int)Enums.CropTypes.SaladOnions || 
                cropTypeId == (int)Enums.CropTypes.Parsnips || 
                cropTypeId == (int)Enums.CropTypes.RunnerBeans || 
                cropTypeId == (int)Enums.CropTypes.Sweetcorn || 
                cropTypeId == (int)Enums.CropTypes.Turnips || 
                cropTypeId == (int)Enums.CropTypes.Beetroot || 
                cropTypeId == (int)Enums.CropTypes.BrusselSprouts || 
                cropTypeId == (int)Enums.CropTypes.Cabbage || 
                cropTypeId == (int)Enums.CropTypes.Calabrese || 
                cropTypeId == (int)Enums.CropTypes.Cauliflower || 
                cropTypeId == (int)Enums.CropTypes.Leeks || 
                cropTypeId == (int)Enums.CropTypes.Grass || 
                cropTypeId == (int)Enums.CropTypes.WholecropSpringBarley || 
                cropTypeId == (int)Enums.CropTypes.WholecropSpringWheat || 
                cropTypeId == (int)Enums.CropTypes.WholecropWinterBarley || 
                cropTypeId == (int)Enums.CropTypes.WholecropWinterWheat))
            {                
                nMaxLimit = nMaxLimit + 80;
            }

            return nMaxLimit;
        }
    }
}
