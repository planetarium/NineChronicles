using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class WorldBossListSheet : Sheet<int, WorldBossListSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public int BossId;
            public long StartedBlockIndex;
            public long EndedBlockIndex;
            public int EntranceFee;
            public int TicketPrice;
            public int AdditionalTicketPrice;
            public int MaxPurchaseCount;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                StartedBlockIndex = ParseLong(fields[2]);
                EndedBlockIndex = ParseLong(fields[3]);
                EntranceFee = ParseInt(fields[4]);
                TicketPrice = ParseInt(fields[5]);
                AdditionalTicketPrice = ParseInt(fields[6]);
                MaxPurchaseCount = ParseInt(fields[7]);
            }
        }

        public WorldBossListSheet() : base(nameof(WorldBossListSheet))
        {
        }
    }
}
