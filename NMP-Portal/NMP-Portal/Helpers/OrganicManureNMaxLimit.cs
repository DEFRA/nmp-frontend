using NMP.Portal.Resources;

namespace NMP.Portal.Helpers
{
    public class OrganicManureNMaxLimit
    {
        public int NMaxLimit(int nMaxLimit, decimal yield, string soilType, string cropInfo1, int cropTypeId, List<int> currentYearManureTypeIds,
            List<int> previousYearManureTypeIds, int manureTypeId)
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
                if (cropInfo1 == Resource.lblMilling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 8.0m)
                {
                    nMaxLimit = (int)Math.Round(((yield - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringWheat)
            {
                if (cropInfo1 == Resource.lblMilling)
                {
                    nMaxLimit = nMaxLimit + 40;
                }
                if (yield > 7.0m)
                {
                    nMaxLimit = (int)Math.Round(((yield - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterBarley)
            {
                if (cropInfo1 == Resource.lblShallow)
                {
                    nMaxLimit = nMaxLimit + 20;
                }
                if (yield > 6.5m)
                {
                    nMaxLimit = nMaxLimit + (int)Math.Round(((yield - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringBarley)
            {
                if (yield > 5.5m)
                {
                    nMaxLimit = (int)Math.Round(((yield - 8.0m) / 0.1m) * 2);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
            {
                if (yield > 3.5m)
                {
                    nMaxLimit = (int)Math.Round(((yield - 8.0m) / 0.1m) * 6);
                }
            }
            else if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.Grass)
            {
                //IF 3 or more ‘Cuts’ 
                // nMaxLimit = nMaxLimit + 40;
            }

            if (cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterWheat || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringWheat
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterBarley || cropTypeId == (int)NMP.Portal.Enums.CropTypes.SpringBarley
                || cropTypeId == (int)NMP.Portal.Enums.CropTypes.WinterOilseedRape)
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
