using Libplanet.Types.Assets;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class RuneCostSheetExtension
    {
        public static int GetMaxTryCount(
            this RuneCostSheet.Row costRow,
            int startLevel,
            (FungibleAssetValue ncg, FungibleAssetValue crystal, FungibleAssetValue rune) balance,
            int limit)
        {
            (long ncg, long crystal, long runeStone) quantity = (0, 0, 0);

            int tryCount;
            for (tryCount = 0; tryCount < limit; tryCount++)
            {
                if (!costRow.TryGetCost(startLevel + tryCount + 1, out var cost))
                {
                    break;
                }

                var ncgCost = (quantity.ncg + cost.NcgQuantity) * balance.ncg.Currency;
                var crystalCost = (quantity.crystal + cost.CrystalQuantity) * balance.crystal.Currency;
                var runeStoneCost = (quantity.runeStone + cost.RuneStoneQuantity) * balance.rune.Currency;

                if (balance.ncg < ncgCost ||
                    balance.crystal < crystalCost ||
                    balance.rune < runeStoneCost)
                {
                    break;
                }

                quantity.ncg += cost.NcgQuantity;
                quantity.crystal += cost.CrystalQuantity;
                quantity.runeStone += cost.RuneStoneQuantity;
            }

            return tryCount;
        }

        public static (long ncg, long crystal, long rune) GetCostQuantity(
            this RuneCostSheet.Row costRow,
            int startLevel,
            int tryCount)
        {
            (long ncg, long crystal, long runeStone) quantity = (0, 0, 0);

            for (var levelUpCount = 1; levelUpCount <= tryCount; levelUpCount++)
            {
                if (!costRow.TryGetCost(startLevel + levelUpCount, out var cost))
                {
                    break;
                }

                quantity.ncg += cost.NcgQuantity;
                quantity.crystal += cost.CrystalQuantity;
                quantity.runeStone += cost.RuneStoneQuantity;
            }

            return quantity;
        }

        public static long GetCostQuantity(
            this RuneCostSheet.Row costRow,
            int startLevel,
            int tryCount,
            RuneCostType costType)
        {
            long quantity = 0;
            for (var levelUpCount = 1; levelUpCount <= tryCount; levelUpCount++)
            {
                if (!costRow.TryGetCost(startLevel + levelUpCount, out var cost))
                {
                    break;
                }

                quantity += costType switch
                {
                    RuneCostType.Ncg => cost.NcgQuantity,
                    RuneCostType.Crystal => cost.CrystalQuantity,
                    RuneCostType.RuneStone => cost.RuneStoneQuantity,
                    _ => 0
                };
            }

            return quantity;
        }
    }
}
