using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class BattleQuest : Quest
    {
        public BattleQuest(BattleQuestSheet.Row data) : base(data)
        {
        }

        public override void Check(Player player, List<ItemBase> items)
        {
            if (Complete)
                return;
            Complete = player.worldStage > Data.Goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeBattleQuestInfo(Data.Goal);
        }
    }
}
