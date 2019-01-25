using System;
using System.Linq;
using Nekoyume.Game;
using Nekoyume.Game.Character;

namespace Nekoyume.Model
{
    [Serializable]
    public class Attack : EventBase
    {
        public int atk;

        public override void Execute(Stage stage)
        {
            Game.Character.CharacterBase attacker;
            Game.Character.CharacterBase defender;
            var player = stage.GetComponentInChildren<Game.Character.Player>();
            var enemies = stage.GetComponentsInChildren<Enemy>();
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
