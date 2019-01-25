using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class StartStage : EventBase
    {
        public int stage;

        public override void Execute(Stage stage)
        {
            stage.StageEnter(this.stage);
        }
    }
}
