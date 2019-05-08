using System.Collections.Generic;
using Nekoyume.Model;

namespace Nekoyume.Action
{
    public class MonsterWave
    {
        private readonly List<Monster> _monsters = new List<Monster>();
        public bool IsBoss;
        public long EXP;


        public void Add(Monster monster)
        {
            _monsters.Add(monster);
        }

        public void Spawn(Simulator simulator)
        {
            foreach (var monster in _monsters)
            {
                simulator.Player.targets.Add(monster);
                simulator.Characters.Enqueue(monster, Simulator.Speed / monster.TurnSpeed);
                monster.InitAI();
            }

            var spawnWave = new SpawnWave
            {
                monsters = _monsters,
                isBoss = IsBoss
            };
            simulator.Log.Add(spawnWave);
        }
    }
}
