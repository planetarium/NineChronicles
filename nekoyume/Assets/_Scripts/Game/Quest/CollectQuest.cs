using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CollectQuest : Quest
    {
        public int itemId;
        public int current;

        public CollectQuest(Data.Table.Quest data) : base(data)
        {
            var collectQuest = (Data.Table.CollectQuest) data;
            itemId = collectQuest.itemId;
        }

        public override void Check(Player player, List<List<ItemBase>> rewards)
        {
            if (Complete)
                return;
            Update(rewards);
            Complete = current >= goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeCollectQuestInfo(itemId, current, goal);
        }

        private void Update(List<List<ItemBase>> rewards)
        {
            var items = rewards.SelectMany(i => i).Where(i => i.Data.id == itemId).ToList();
            var itemCount = items.Count();
            current += itemCount;
        }
    }
}
