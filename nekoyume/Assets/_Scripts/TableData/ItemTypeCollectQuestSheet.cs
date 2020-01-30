using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemTypeCollectQuestSheet: Sheet<int, ItemTypeCollectQuestSheet.Row>
    {
        [Serializable]
        public class Row : QuestSheet.Row
        {
            public ItemType ItemType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                ItemType = (ItemType) Enum.Parse(typeof(ItemType), fields[3]);
            }
        }

        public ItemTypeCollectQuestSheet() : base(nameof(ItemTypeCollectQuestSheet))
        {
        }
    }
}
