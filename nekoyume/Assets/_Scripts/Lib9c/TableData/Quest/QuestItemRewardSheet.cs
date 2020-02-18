using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class QuestItemRewardSheet : Sheet<int,QuestItemRewardSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            public int ItemId { get; private set; }
            public int Count { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ItemId = int.Parse(fields[1]);
                Count = int.Parse(fields[2]);
            }
        }

        public QuestItemRewardSheet() : base(nameof(QuestItemRewardSheet))
        {
        }
    }
}
