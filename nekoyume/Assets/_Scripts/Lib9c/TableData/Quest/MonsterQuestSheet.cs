using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class MonsterQuestSheet : Sheet<int, MonsterQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int MonsterId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                TryParseInt(fields[3], out var monsterId);
                MonsterId = monsterId;
            }
        }

        public MonsterQuestSheet() : base(nameof(MonsterQuestSheet))
        {
        }
    }
}
