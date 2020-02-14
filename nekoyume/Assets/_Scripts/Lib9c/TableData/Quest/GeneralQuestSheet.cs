using System;
using System.Collections.Generic;
using Nekoyume.Model.Quest;

namespace Nekoyume.TableData
{
    [Serializable]
    public class GeneralQuestSheet : Sheet<int, GeneralQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public QuestEventType Event { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Event = (QuestEventType) Enum.Parse(typeof(QuestEventType), fields[3]);
            }
        }

        public GeneralQuestSheet() : base(nameof(GeneralQuestSheet))
        {
        }
    }
}
