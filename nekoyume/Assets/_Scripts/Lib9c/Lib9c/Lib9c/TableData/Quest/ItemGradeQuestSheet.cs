using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemGradeQuestSheet : Sheet<int, ItemGradeQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int Grade { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                TryParseInt(fields[3], out var grade);
                Grade = grade;
            }
        }

        public ItemGradeQuestSheet() : base(nameof(ItemGradeQuestSheet))
        {
        }
    }
}
