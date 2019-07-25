using System;
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
            return $"스테이지 {goal} 클리어 하기";
        }
    }
}
