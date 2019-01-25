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
        public override void Execute(Stage stage)
        {
            if (character is Player)
            {
                var player = stage.GetComponentInChildren<Game.Character.Player>();
                player.Die();
            }
            else
            {
                var enemies = stage.GetComponentsInChildren<Enemy>();
                var enemy = enemies.FirstOrDefault(e => e.id == characterId);
                if (enemy != null)
                {
                    enemy.Die();
                }
            }
        }
    }
}
