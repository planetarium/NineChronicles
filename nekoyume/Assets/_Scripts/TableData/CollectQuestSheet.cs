using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CollectQuestSheet : Sheet<int, CollectQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public int ItemId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                ItemId = int.Parse(fields[3]);
            }
        }
        
        public CollectQuestSheet() : base(nameof(CollectQuestSheet))
        {
        }
    }
}
