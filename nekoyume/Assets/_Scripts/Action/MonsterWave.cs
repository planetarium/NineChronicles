using System.Collections.Generic;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    public class MonsterWave
    {
        public readonly List<Monster> monsters = new List<Monster>();


        public void Add(Monster monster)
        {
            monsters.Add(monster);
        }

        public void Spawn(Simulator simulator)
        {
            foreach (var monster in monsters)
            {
                simulator.Player.targets.Add(monster);
                simulator.Characters.Add(monster);
                monster.InitAI();
            }

            var spawnWave = new SpawnWave
            {
                monsters = monsters,
            };
            simulator.Log.Add(spawnWave);
        }
    }
}
