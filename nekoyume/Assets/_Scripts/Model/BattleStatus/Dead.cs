using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Dead : EventBase
    {
        public override void Execute(Game.Character.Player player, IEnumerable<Enemy> enemies)
        {
            if (character is Player)
            {
                player.Dead();
            }
            else
            {
                var enemy = enemies.OfType<Enemy>().FirstOrDefault(e => e.id == characterId);
                if (enemy != null)
                {
                    enemy.Dead();
                }
            }
        }
    }
}
