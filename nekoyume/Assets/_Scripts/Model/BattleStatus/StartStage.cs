using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class StartStage : EventBase
    {
        public int stage;

        public override void Execute(Game.Character.Player player, IEnumerable<Enemy> enemies)
        {
        }
    }
}
