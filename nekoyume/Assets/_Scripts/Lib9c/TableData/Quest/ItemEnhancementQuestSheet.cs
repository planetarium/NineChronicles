using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemEnhancementQuestSheet : Sheet<int, ItemEnhancementQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int Grade { get; private set; }
            public int Count { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                TryParseInt(fields[3], out var grade);
                Grade = grade;
                TryParseInt(fields[4], out var count);
                Count = count;
            }
        }

        public ItemEnhancementQuestSheet() : base(nameof(ItemEnhancementQuestSheet))
        {
        }
    }
}
