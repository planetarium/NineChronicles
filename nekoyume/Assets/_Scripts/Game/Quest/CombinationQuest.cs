using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CombinationQuest : Quest
    {
        public int current;
        public readonly ItemType ItemType;
        public readonly ItemSubType ItemSubType;

        public override QuestType QuestType => QuestType.Craft;

        public CombinationQuest(CombinationQuestSheet.Row data) : base(data)
        {
            ItemType = data.ItemType;
            ItemSubType = data.ItemSubType;
        }

        public CombinationQuest(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
            current = (int) ((Integer) serialized[(Bencodex.Types.Text) "current"]).Value;
            ItemType = (ItemType) (int) ((Integer) serialized[(Bencodex.Types.Text) "itemType"]).Value;
            ItemSubType = (ItemSubType) (int) ((Integer) serialized[(Bencodex.Types.Text) "itemSubType"]).Value;
        }

        public override void Check()
        {
            if (Complete)
                return;
            Complete = current >= Goal;
        }

        public override string ToInfo()
        {
            return string.Format(GoalFormat, GetName(), current, Goal);
        }

        public override string GetName()
        {
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            return string.Format(format, ItemSubType.GetLocalizedString());
        }

        protected override string TypeId => "combinationQuest";

        public void Update(List<ItemBase> items)
        {
            current += items.Count(i => i.Data.ItemType == ItemType && i.Data.ItemSubType == ItemSubType);
            Check();
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "current"] = (Integer) current,
                [(Text) "itemType"] = (Integer) (int) ItemType,
                [(Text) "itemSubType"] = (Integer) (int) ItemSubType,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
