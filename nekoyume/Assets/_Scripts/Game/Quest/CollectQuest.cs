using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CollectQuest : Quest
    {
        public int current;
        
        public new CollectQuestSheet.Row Data { get; }

        public CollectQuest(CollectQuestSheet.Row data) : base(data)
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
            var format = LocalizationManager.Localize("QUEST_COLLECT_CURRENT_INFO_FORMAT");
            var itemName = LocalizationManager.LocalizeItemName(Data.ItemId);
            return string.Format(format, itemName, current, Data.Goal);
        }

        private void Update(List<ItemBase> rewards)
        {
            current += rewards.Count(i => i.Data.Id == Data.ItemId);
        }
    }
}
