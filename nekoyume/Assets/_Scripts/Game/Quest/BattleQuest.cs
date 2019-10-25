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
        public override QuestType QuestType => QuestType.Adventure;

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
            var format = LocalizationManager.Localize("QUEST_BATTLE_CURRENT_INFO_FORMAT");
            return string.Format(format, Data.Goal);
        }
    }
}
