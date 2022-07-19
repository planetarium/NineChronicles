using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class WorldBossRankRewardSheet: Sheet<int, WorldBossRankRewardSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public int BossId;
            public int Rank;
            public int Rune;
            public int Crystal;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                Rank = ParseInt(fields[2]);
                Rune = ParseInt(fields[3]);
                Crystal = ParseInt(fields[4]);
            }
        }

        public WorldBossRankRewardSheet() : base(nameof(WorldBossRankRewardSheet))
        {
        }
    }
}
