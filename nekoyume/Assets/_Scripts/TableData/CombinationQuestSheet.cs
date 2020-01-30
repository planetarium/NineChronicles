using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class CombinationQuestSheet : Sheet<int, CombinationQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public ItemType ItemType { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                ItemType = (ItemType) Enum.Parse(typeof(ItemType), fields[3]);
                ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[4]);
            }
        }
        
        public CombinationQuestSheet() : base(nameof(CombinationQuestSheet))
        {
        }
    }
}
