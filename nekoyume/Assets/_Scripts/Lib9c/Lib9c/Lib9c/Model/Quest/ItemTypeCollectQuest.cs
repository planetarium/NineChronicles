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
        public readonly List<int> ItemIds = new List<int>();

        public ItemTypeCollectQuest(ItemTypeCollectQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            ItemType = data.ItemType;
        }

        public ItemTypeCollectQuest(Dictionary serialized) : base(serialized)
        {
            ItemIds = serialized["itemIds"].ToList(i => i.ToInteger());
            ItemType = serialized["itemType"].ToEnum<ItemType>();
        }

        public void Update(ItemBase item)
        {
            if (Complete)
                return;

            if (!ItemIds.Contains(item.Id))
            {
                _current++;
                ItemIds.Add(item.Id);
                ItemIds.Sort();
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
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemType"] = ItemType.Serialize(),
                [(Text)"itemIds"] = new List(ItemIds.OrderBy(i => i).Select(i => i.Serialize())),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
