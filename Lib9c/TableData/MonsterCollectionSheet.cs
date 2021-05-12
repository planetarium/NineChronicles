using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MonsterCollectionSheet : Sheet<int, MonsterCollectionSheet.Row>
    {
        [Serializable]
        public class Row: SheetRow<int>
        {
            public override int Key => Level;
            public int Level { get; private set; }
            public int RequiredGold { get; private set; }
            public int RewardId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                RequiredGold = ParseInt(fields[1]);
                RewardId = ParseInt(fields[2]);
            }
        }

        public MonsterCollectionSheet() : base(nameof(MonsterCollectionSheet))
        {
        }
    }
}
