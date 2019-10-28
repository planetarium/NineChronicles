using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CombinationQuest : Quest
    {
        public int current;

        public new CombinationQuestSheet.Row Data { get; }
        public override QuestType QuestType => QuestType.Craft;

        public CombinationQuest(CombinationQuestSheet.Row data) : base(data)
        {
            Data = data;
        }

        public override void Check(Player player, List<ItemBase> items)
        {
            if (Complete)
                return;
            Update(items);
            Complete = current >= Data.Goal;
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("QUEST_COMBINATION_CURRENT_INFO_FORMAT");
            return string.Format(format, Data.ItemSubType.GetLocalizedString(), current, Data.Goal);
        }

        private void Update(List<ItemBase> items)
        {
            current += items.Count(i => i.Data.ItemType == Data.ItemType &&
                                        i.Data.ItemSubType == Data.ItemSubType);
        }
    }
}
