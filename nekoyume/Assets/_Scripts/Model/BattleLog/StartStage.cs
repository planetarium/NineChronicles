using System;

namespace Nekoyume.Model.BattleLog
{
    [Serializable]
    public class StartStage : LogBase
    {
        public int stage;

        public StartStage(int stage)
        {
            this.stage = stage;
        }
    }
}
