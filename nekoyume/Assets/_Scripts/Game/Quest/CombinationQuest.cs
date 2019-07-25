using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class CombinationQuest : Quest
    {
        public int current;
        public string cls;

        public CombinationQuest(Data.Table.Quest data) : base(data)
        {
            var questData = (Data.Table.CombinationQuest) data;
            cls = questData.cls;
        }

        public override void Check(Player player, List<ItemBase> items)
        {
            if (Complete)
                return;
            Update(items);
            Complete = current >= goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeCombinationQuestInfo(cls, current, goal);
        }

        private void Update(List<ItemBase> items)
        {
            current += items.Count(i => i.Data.cls == cls);
        }
    }
}
