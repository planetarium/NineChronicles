using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Battle;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class RuneHelper
    {
        public static Currency ToCurrency(int runeId)
        {
            return new Currency(runeId.ToString(CultureInfo.InvariantCulture), decimalPlaces: 0, minters: null);
        }

        public static FungibleAssetValue ToFungibleAssetValue(int runeId, int quantity)
        {
            return ToCurrency(runeId) * quantity;
        }

        public static List<FungibleAssetValue> CalculateReward(int rank, int bossId, RuneWeightSheet sheet, WorldBossRankRewardSheet rewardSheet, IRandom random)
        {
            var row = sheet.Values.First(r => r.Rank == rank && r.BossId == bossId);
            var rewardRow = rewardSheet.Values.First(r => r.Rank == rank && r.BossId == bossId);
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
                .Select(kv => ToFungibleAssetValue(kv.Key, kv.Value))
                .ToList();
            result.Add(rewardRow.Crystal * CrystalCalculator.CRYSTAL);
            return result;
        }
    }
}
