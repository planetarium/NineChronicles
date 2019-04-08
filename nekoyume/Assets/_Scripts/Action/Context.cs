using System;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [Serializable]
    public class Context
    {
        public Avatar avatar;
        public BattleLog battleLog;

        public Context(Avatar avatar, BattleLog logs = null)
        {
            this.avatar = avatar;
            battleLog = logs;
        }
    }
}
