using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class LevelUp : EventBase
    {
        public override bool skip => true;

        public override void Execute(IStage stage)
        {
        }
    }
}
