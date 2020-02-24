using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

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
                Id = ParseInt(fields[0]);
                ItemId = ParseInt(fields[1]);
                Count = ParseInt(fields[2]);
            }
        }

        public QuestItemRewardSheet() : base(nameof(QuestItemRewardSheet))
        {
        }
    }
}
