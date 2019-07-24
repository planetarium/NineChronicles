using System;
using Assets.SimpleLocalization;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class BattleQuest : Quest
    {
        public BattleQuest(Data.Table.Quest data) : base(data)
        {
        }

        public override void Check(Player player)
        {
            if (Complete)
                return;
            Complete = player.worldStage >= goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeBattleQuestInfo(goal);
        }
    }
}
