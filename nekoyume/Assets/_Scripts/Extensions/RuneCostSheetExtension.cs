using Libplanet.Types.Assets;
using Nekoyume.EnumType;
using Nekoyume.Helper;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class RuneCostSheetExtension
    {
        /// <summary>
        /// 지금 보내면 들어갈 블록(다음 블록) 기준으로 할인된 한 번의 강화 비용. 체인은 소환 룬의 크리스탈·룬스톤 비용을
        /// BoostScheduleSheet 로 깎아 과금하므로, 표시·가능 횟수·부족 판정도 이 값을 써야 한다.
        /// </summary>
        public static bool TryGetBoostedCost(
            this RuneCostSheet.Row costRow,
            int level,
            out RuneCostSheet.RuneCostData cost)
        {
            if (!costRow.TryGetCost(level, out cost))
            {
                return false;
            }

            var (boost, blockIndex) = ResolveBoost(costRow);
            cost = RuneHelper.ApplyEnhancementCostBoost(cost, costRow.RuneId, level, boost, blockIndex);
            return true;
        }

        /// <summary>
        /// 이 룬에 <paramref name="blockIndex"/>(기본: 다음 블록)에 걸린 강화 비용 부스트(없으면 null). 룬마다 따로
        /// 해석해야 한다 — 해석한 룬에만 적용된다.
        /// </summary>
        private static (RuneEnhancementCostBoost boost, long blockIndex) ResolveBoost(
            RuneCostSheet.Row costRow,
            long? blockIndex = null)
        {
            var game = Game.Game.instance;
            var tableSheets = game?.TableSheets;
            if (tableSheets?.BoostScheduleSheet is null || game.Agent is null)
            {
                return (null, 0);
            }

            var resolvedBlockIndex = blockIndex ?? BoostScheduleFrontHelper.NextBlockIndex;
            var boost = RuneHelper.ResolveEnhancementCostBoost(
                tableSheets.BoostScheduleSheet,
                costRow.RuneId,
                resolvedBlockIndex,
                () => tableSheets.RuneSummonSheet);
            return (boost, resolvedBlockIndex);
        }

        /// <param name="costRow">룬 비용 행.</param>
        /// <param name="startLevel">지금 레벨.</param>
        /// <param name="balance">가진 NCG·크리스털·룬스톤.</param>
        /// <param name="limit">최대 시도 횟수 상한.</param>
        /// <param name="blockIndex">비용을 셀 블록. 기본은 다음 블록(지금 보내면 들어갈 블록).</param>
        public static int GetMaxTryCount(
            this RuneCostSheet.Row costRow,
            int startLevel,
            (FungibleAssetValue ncg, FungibleAssetValue crystal, FungibleAssetValue rune) balance,
            int limit,
            long? blockIndex = null)
        {
            (long ncg, long crystal, long runeStone) quantity = (0, 0, 0);
            var (boost, resolvedBlockIndex) = ResolveBoost(costRow, blockIndex);

            int tryCount;
            for (tryCount = 0; tryCount < limit; tryCount++)
            {
                var level = startLevel + tryCount + 1;
                if (!costRow.TryGetCost(level, out var cost))
                {
                    break;
                }

                cost = RuneHelper.ApplyEnhancementCostBoost(cost, costRow.RuneId, level, boost, resolvedBlockIndex);

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
            var (boost, blockIndex) = ResolveBoost(costRow);

            for (var levelUpCount = 1; levelUpCount <= tryCount; levelUpCount++)
            {
                var level = startLevel + levelUpCount;
                if (!costRow.TryGetCost(level, out var cost))
                {
                    break;
                }

                cost = RuneHelper.ApplyEnhancementCostBoost(cost, costRow.RuneId, level, boost, blockIndex);

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
            var (boost, blockIndex) = ResolveBoost(costRow);
            for (var levelUpCount = 1; levelUpCount <= tryCount; levelUpCount++)
            {
                var level = startLevel + levelUpCount;
                if (!costRow.TryGetCost(level, out var cost))
                {
                    break;
                }

                cost = RuneHelper.ApplyEnhancementCostBoost(cost, costRow.RuneId, level, boost, blockIndex);

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
