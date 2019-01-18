using System;
using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Model.BattleLog;

namespace Nekoyume.Action
{
    [Serializable]
    public class Context
    {
        public Avatar avatar;
        public List<LogBase> battleLog;

        public Context(Avatar avatar, List<LogBase> logs = null)
        {
            this.avatar = avatar;
            battleLog = logs;
        }
    }
}
