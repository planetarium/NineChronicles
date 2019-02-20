using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.UI;

namespace Nekoyume.Model
{
    [Serializable]
    public class BattleResult : EventBase
    {
        public Result result;
        public enum Result
        {
            Win,
            Lose
        }

        public override bool skip => false;

        public override void Execute(IStage stage)
        {
            stage.StageEnd(result);
        }
    }
}
