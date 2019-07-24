using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class BattleQuest : Quest
    {
        public BattleQuest(Data.Table.Quest data) : base(data)
        {
        }

        public override void Check(Player player, List<List<ItemBase>> rewards)
        {
            if (Complete)
                return;
            Complete = player.worldStage > goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeBattleQuestInfo(goal);
        }
    }
}
