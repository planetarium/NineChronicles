using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class ItemTypeCollectQuest : Quest
    {
        public readonly ItemType ItemType;
        private readonly List<int> _itemIds = new List<int>();


        public ItemTypeCollectQuest(ItemTypeCollectQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
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

            if (!_itemIds.Contains(item.Id))
            {
                _current++;
                _itemIds.Add(item.Id);
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

        // FIXME: 이 메서드 구현은 중복된 코드가 다른 데서도 많이 있는 듯.
        public override string GetProgressText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                GoalFormat,
                Math.Min(Goal, _current),
                Goal
            );

        protected override string TypeId => "itemTypeCollectQuest";

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemType"] = (Integer)(int)ItemType,
                [(Text)"itemIds"] = (List)_itemIds.Select(i => (Integer)i).Serialize(),
            }.Union((Dictionary)base.Serialize()));

    }
}
