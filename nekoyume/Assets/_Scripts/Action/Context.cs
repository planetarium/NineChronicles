using System;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [Serializable]
    public class Context
    {
        public Avatar avatar;
        public BattleLog battleLog;
        public int gold;

        public Context(Avatar avatar, BattleLog logs = null, int gold = 0)
        {
            this.avatar = avatar;
            battleLog = logs;
            this.gold = gold;
        }
    }
}
