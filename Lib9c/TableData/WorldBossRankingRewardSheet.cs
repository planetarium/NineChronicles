using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.Helper;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    // This sheet not on-chain data. don't call this sheet in `IAction.Execute()`
    public class WorldBossRankingRewardSheet : Sheet<int, WorldBossRankingRewardSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public struct RuneInfo
            {
                public int RuneId;
                public int RuneQty;

                public RuneInfo(int id, int qty)
                {
                    RuneId = id;
                    RuneQty = qty;
                }
            }

            public int Id;
            public int BossId;
            public int RankingMin;
            public int RankingMax;
            public int RateMin;
            public int RateMax;
            public List<RuneInfo> Runes;
            public int Crystal;
            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                RankingMin = ParseInt(fields[2]);
                RankingMax = ParseInt(fields[3]);
                RateMin = ParseInt(fields[4]);
                RateMax = ParseInt(fields[5]);
                Runes = new List<RuneInfo>();
                for (int i = 0; i < 3; i++)
                {
                    var offset = i * 2;
                    Runes.Add(new RuneInfo(ParseInt(fields[6 + offset]), ParseInt(fields[7 + offset])));
                }
                Crystal = ParseInt(fields[12]);
            }

            public List<FungibleAssetValue> GetRewards(RuneSheet runeSheet)
            {
                var result = new List<FungibleAssetValue>
                {
                    Crystal * CrystalCalculator.CRYSTAL
                };
                result.AddRange(Runes
                    .Where(runeInfo => runeInfo.RuneQty > 0)
                    .Select(runeInfo =>
                        RuneHelper.ToFungibleAssetValue(runeSheet[runeInfo.RuneId],
                            runeInfo.RuneQty)));
                return result;
            }
        }

        public WorldBossRankingRewardSheet() : base(nameof(WorldBossRankingRewardSheet))
        {
        }

        public Row FindRow(int ranking, int rate)
        {
            if (ranking <= 0 && rate <= 0)
            {
                throw new ArgumentException($"ranking or rate must be greater than 0. ranking: {ranking}, rate: {rate}");
            }
            return OrderedList.LastOrDefault(r => r.RankingMin <= ranking && ranking <= r.RankingMax) ?? OrderedList.LastOrDefault(r => r.RateMin <= rate && rate <= r.RateMax);
        }
    }
}
