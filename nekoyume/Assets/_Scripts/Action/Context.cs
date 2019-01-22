using System;
using System.Collections.Generic;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    [Serializable]
    public class Context
    {
        public Avatar avatar;
        public List<BattleLog> battleLog;

        public Context(Avatar avatar, List<BattleLog> logs = null)
        {
            this.avatar = avatar;
            battleLog = logs;
        }
    }
}
