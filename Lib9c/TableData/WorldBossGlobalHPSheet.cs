using System;
using System.Collections.Generic;
using System.Numerics;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldBossGlobalHpSheet : Sheet<int, WorldBossGlobalHpSheet.Row>
    {
        public class Row: SheetRow<int>
        {
            public int Level;
            public BigInteger Hp;
            public override int Key => Level;
            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                Hp = ParseBigInteger(fields[1]);
            }
        }

        public WorldBossGlobalHpSheet() : base(nameof(WorldBossGlobalHpSheet))
        {
        }
    }
}
