using Nekoyume.TableData.Pet;

namespace Nekoyume.Helper
{
    public static class PetHelper
    {
        public static (int ncgQuantity, int soulStoneQuantity) CalculateEnhancementCost(
            PetCostSheet costSheet,
            int petId,
            int nowLevel,
            int targetLevel)
        {
            var costList = costSheet[petId].Cost;
            var range = targetLevel - nowLevel;
            var startIndex = nowLevel != 0
                ? costList.FindIndex(cost => cost.Level == nowLevel) + 1
                : 0;
            var ncgCost = 0;
            var soulStoneCost = 0;
            costList.GetRange(startIndex, range).ForEach(cost =>
            {
                ncgCost += cost.NcgQuantity;
                soulStoneCost += cost.SoulStoneQuantity;
            });
            return (ncgCost, soulStoneCost);
        }
    }
}
