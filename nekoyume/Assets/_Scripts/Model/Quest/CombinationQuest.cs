using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class CombinationQuest : Quest
    {
        public readonly ItemType ItemType;
        public readonly ItemSubType ItemSubType;

        public override QuestType QuestType => QuestType.Craft;

        public CombinationQuest(CombinationQuestSheet.Row data) : base(data)
        {
            ItemType = data.ItemType;
            ItemSubType = data.ItemSubType;
        }

        public CombinationQuest(Dictionary serialized) : base(serialized)
        {
            ItemType = (ItemType)(int)((Integer)serialized["itemType"]).Value;
            ItemSubType = (ItemSubType)(int)((Integer)serialized["itemSubType"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;

            Complete = _current >= Goal;
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_COMBINATION_CURRENT_INFO_FORMAT");
            return string.Format(format, ItemSubType.GetLocalizedString());
        }

        public override string GetProgressText()
        {
            return string.Format(GoalFormat, Math.Min(Goal, _current), Goal);
        }

        protected override string TypeId => "combinationQuest";

        public void Update(List<ItemBase> items)
        {
            if (Complete)
                return;

            _current += items.Count(i => i.Data.ItemType == ItemType && i.Data.ItemSubType == ItemSubType);
            Check();
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"itemType"] = (Integer)(int)ItemType,
                [(Text)"itemSubType"] = (Integer)(int)ItemSubType,
            }.Union((Dictionary)base.Serialize()));

    }
}
