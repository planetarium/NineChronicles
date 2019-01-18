using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class BattleResult : LogBase
    {
        public string result;

        public BattleResult(string result)
        {
            this.result = result;
        }
    }
}
