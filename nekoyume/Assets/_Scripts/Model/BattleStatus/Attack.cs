using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : EventBase
    {
        public int atk;

        public override void Execute(Game.Character.Player player, IEnumerable<Enemy> enemies)
        {
            Game.Character.CharacterBase attacker;
            Game.Character.CharacterBase defender;
            if (character is Player)
            {
                attacker = player;
                defender = enemies.OfType<Enemy>().FirstOrDefault(e => e.id == targetId);
            }
            else
            {
                attacker = enemies.OfType<Enemy>().FirstOrDefault(e => e.id == characterId);
                defender = player;
            }

            if (attacker != null && defender != null)
            {
                attacker.Attack(atk, defender);
            }
        }
    }
}
