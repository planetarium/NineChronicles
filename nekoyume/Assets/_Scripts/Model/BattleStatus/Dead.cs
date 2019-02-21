using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Dead : EventBase
    {

        public override bool skip => true;

        public override void Execute(IStage stage)
        {
            stage.Dead(character);
        }
    }
}
