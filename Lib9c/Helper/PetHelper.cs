using System;
using Nekoyume.TableData;
using Nekoyume.TableData.Pet;

namespace Nekoyume.Helper
{
    public static class PetHelper
    {
        public static (int ncgQuantity, int soulStoneQuantity) CalculateEnhancementCost(
            PetCostSheet costSheet,
            int petId,
            int currentLevel,
            int targetLevel)
        {
            if (costSheet is null)
            {
                throw new ArgumentNullException(nameof(costSheet));
            }

            if (petId < 0)
            {
                throw new ArgumentException(
                    $"{nameof(petId)} must be greater than or equal to 0.");
            }

            if (currentLevel < 0)
            {
                throw new ArgumentException(
                    $"{nameof(currentLevel)} must be greater than or equal to 0.");
            }

            if (targetLevel < 1)
            {
                throw new ArgumentException(
                    $"{nameof(targetLevel)} must be greater than or equal to 0.");
            }

            if (currentLevel >= targetLevel)
            {
                throw new ArgumentException(
                    $"{nameof(currentLevel)} must be less than {nameof(targetLevel)}.");
            }

            if (!costSheet.TryGetValue(petId, out var row))
            {
                throw new SheetRowNotFoundException(nameof(PetCostSheet), petId);
            }

            var costList = row.Cost;
            var range = targetLevel - currentLevel;
            var startIndex = currentLevel != 0
                ? costList.FindIndex(cost => cost.Level == currentLevel) + 1
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
