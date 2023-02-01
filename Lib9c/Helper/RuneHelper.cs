#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class RuneHelper
    {
        public static readonly Currency StakeRune = Currency.Legacy("RUNE_GOLDENLEAF", 0, null);
        public static readonly Currency DailyRewardRune = Currency.Legacy("RUNE_ADVENTURER", 0, null);

        public static Currency ToCurrency(
            RuneSheet.Row runeRow,
            byte decimalPlaces,
            IImmutableSet<Address>? minters
        )
        {

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            return Currency.Legacy(runeRow.Ticker, decimalPlaces, minters);
#pragma warning restore CS0618
        }

        public static FungibleAssetValue ToFungibleAssetValue(
            RuneSheet.Row runeRow,
            int quantity,
            byte decimalPlaces = 0,
            IImmutableSet<Address>? minters = null
        )
        {
            return ToCurrency(runeRow, decimalPlaces, minters) * quantity;
        }

        public static List<FungibleAssetValue> CalculateReward(
            int rank,
            int bossId,
            RuneWeightSheet sheet,
            IWorldBossRewardSheet rewardSheet,
            RuneSheet runeSheet,
            IRandom random
        )
        {
            var row = sheet.Values.First(r => r.Rank == rank && r.BossId == bossId);
            var rewardRow = rewardSheet.OrderedRows.First(r => r.Rank == rank && r.BossId == bossId);
            if (rewardRow is WorldBossKillRewardSheet.Row kr)
            {
                kr.SetRune(random);
            }
            else if (rewardRow is WorldBossBattleRewardSheet.Row rr)
            {
                rr.SetRune(random);
            }
            var total = 0;
            var dictionary = new Dictionary<int, int>();
            while (total < rewardRow.Rune)
            {
                var selector = new WeightedSelector<int>(random);
                foreach (var info in row.RuneInfos)
                {
                    selector.Add(info.RuneId, info.Weight);
                }

                var ids = selector.Select(1);
                foreach (var id in ids)
                {
                    if (dictionary.ContainsKey(id))
                    {
                        dictionary[id] += 1;
                    }
                    else
                    {
                        dictionary[id] = 1;
                    }
                }

                total++;
            }

#pragma warning disable LAA1002
            var result = dictionary
#pragma warning restore LAA1002
                .Select(kv => ToFungibleAssetValue(runeSheet[kv.Key], kv.Value))
                .ToList();

            if (rewardRow.Crystal > 0)
            {
                result.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            }
            return result;
        }

        public static bool TryEnhancement(
            FungibleAssetValue ncg,
            FungibleAssetValue crystal,
            FungibleAssetValue rune,
            Currency ncgCurrency,
            Currency crystalCurrency,
            Currency runeCurrency,
            RuneCostSheet.RuneCostData cost,
            IRandom random,
            int maxTryCount,
            out int tryCount)
        {
            tryCount = 0;
            var value = cost.LevelUpSuccessRate + 1;
            while (value > cost.LevelUpSuccessRate)
            {
                tryCount++;
                if (tryCount > maxTryCount)
                {
                    tryCount = maxTryCount;
                    return false;
                }

                if (!CheckBalance(ncg, crystal, rune, ncgCurrency, crystalCurrency, runeCurrency, cost, tryCount))
                {
                    return false;
                }

                value = random.Next(1, GameConfig.MaximumProbability + 1);
            }

            return true;
        }

        private static bool CheckBalance(
            FungibleAssetValue ncg,
            FungibleAssetValue crystal,
            FungibleAssetValue rune,
            Currency ncgCurrency,
            Currency crystalCurrency,
            Currency runeCurrency,
            RuneCostSheet.RuneCostData cost,
            int tryCount)
        {
            var ncgCost = tryCount * cost.NcgQuantity * ncgCurrency;
            var crystalCost = tryCount * cost.CrystalQuantity * crystalCurrency;
            var runeCost = tryCount * cost.RuneStoneQuantity * runeCurrency;
            if (ncg < ncgCost || crystal < crystalCost || rune < runeCost)
            {
                if (tryCount == 1)
                {
                    throw new NotEnoughFungibleAssetValueException($"{nameof(RuneHelper)}" +
                        $"[ncg:{ncg} < {ncgCost}] [crystal:{crystal} < {crystalCost}] [rune:{rune} < {runeCost}]");
                }

                return false;
            }

            return true;
        }

        public static FungibleAssetValue CalculateStakeReward(FungibleAssetValue stakeAmount, int rate)
        {
            var (quantity, _) = stakeAmount.DivRem(stakeAmount.Currency * rate);
            return StakeRune * quantity;
        }
    }
}
