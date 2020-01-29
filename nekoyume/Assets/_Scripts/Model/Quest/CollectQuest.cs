using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class CollectQuest : Quest
    {
        public override QuestType QuestType => QuestType.Obtain;

        private readonly int _itemId;

        public CollectQuest(CollectQuestSheet.Row data, QuestReward reward) 
            : base(data, reward)
        {
            _itemId = data.ItemId;
        }

        public CollectQuest(Dictionary serialized) : base(serialized)
        {
            _itemId = (int)((Integer)serialized["itemId"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            var itemName = LocalizationManager.LocalizeItemName(_itemId);
            return string.Format(format, itemName);
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "collectQuest";

        public void Update(CollectionMap itemMap)
        {
            if (Complete)
                return;

            itemMap.TryGetValue(_itemId, out _current);
            Check();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemId"] = (Integer)_itemId,
            }.Union((Dictionary)base.Serialize()));
    }
}
