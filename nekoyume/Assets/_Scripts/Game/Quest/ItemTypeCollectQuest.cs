using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class ItemTypeCollectQuest : Quest
    {
        public readonly ItemType ItemType;
        private int _count;
        private readonly List<int> _itemIds = new List<int>();


        public ItemTypeCollectQuest(ItemTypeCollectQuestSheet.Row data) : base(data)
        {
            ItemType = data.ItemType;
        }

        public ItemTypeCollectQuest(Dictionary serialized) : base(serialized)
        {
            _count = (int) ((Integer) serialized[(Bencodex.Types.Text) "count"]).Value;
            _itemIds = serialized[(Bencodex.Types.Text) "itemIds"].ToList(i => (int) ((Integer) i).Value);
            ItemType = (ItemType) (int) ((Integer) serialized[(Bencodex.Types.Text) "itemType"]).Value;
        }

        public void Update(ItemBase item)
        {
            if (!_itemIds.Contains(item.Data.Id))
            {
                _count++;
                _itemIds.Add(item.Data.Id);
            }
            Check();
        }

        public override QuestType QuestType => QuestType.Obtain;

        public override void Check()
        {
            Complete = _count >= Goal;
        }

        public override string ToInfo()
        {
            return string.Format(GoalFormat, GetName(), _count, Goal);
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_ITEM_TYPE_FORMAT");
            return string.Format(format, ItemType.GetLocalizedString());
        }

        protected override string TypeId => "itemTypeCollectQuest";

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "count"] = (Integer) _count,
                [(Text) "itemType"] = (Integer) (int) ItemType,
                [(Text) "itemIds"] = (Bencodex.Types.List) _itemIds.Select(i => (Integer) i).Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
