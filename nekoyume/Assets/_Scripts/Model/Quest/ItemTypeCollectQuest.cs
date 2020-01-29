using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class ItemTypeCollectQuest : Quest
    {
        public readonly ItemType ItemType;
        private readonly List<int> _itemIds = new List<int>();


        public ItemTypeCollectQuest(ItemTypeCollectQuestSheet.Row data) : base(data)
        {
            ItemType = data.ItemType;
        }

        public ItemTypeCollectQuest(Dictionary serialized) : base(serialized)
        {
            _itemIds = serialized["itemIds"].ToList(i => (int)((Integer)i).Value);
            ItemType = (ItemType)(int)((Integer)serialized["itemType"]).Value;
        }

        public void Update(ItemBase item)
        {
            if (Complete)
                return;

            if (!_itemIds.Contains(item.Data.Id))
            {
                _current++;
                _itemIds.Add(item.Data.Id);
            }

            Check();
        }

        public override QuestType QuestType => QuestType.Obtain;

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_ITEM_TYPE_FORMAT");
            return string.Format(format, ItemType.GetLocalizedString());
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "itemTypeCollectQuest";

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemType"] = (Integer)(int)ItemType,
                [(Text)"itemIds"] = (List)_itemIds.Select(i => (Integer)i).Serialize(),
            }.Union((Dictionary)base.Serialize()));

    }
}
