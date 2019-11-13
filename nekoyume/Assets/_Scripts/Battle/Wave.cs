using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using UnityEngine;

namespace Nekoyume.Battle
{
    public class Wave
    {
        private readonly List<Enemy> _enemies = new List<Enemy>();
        public bool IsBoss;
        public long Exp;

        public void Add(Enemy enemy)
        {
            _enemies.Add(enemy);
        }

        public void Spawn(Simulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAI();
            }

            var enemies = _enemies.Select(enemy => (Enemy) enemy.Clone()).ToList();
            var spawnWave = new SpawnWave(null, enemies, IsBoss);
            simulator.Log.Add(spawnWave);
        }
    }
}
