using System;
using System.Collections.Generic;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class LevelUp : EventBase
    {
        public override void Execute(Game.Character.Player player, IEnumerable<Enemy> enemies)
        {
        }
    }
}
