using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;

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

        public void Spawn(StageSimulator stageSimulator)
        {
            foreach (var enemy in _enemies)
            {
                stageSimulator.Player.Targets.Add(enemy);
                stageSimulator.Characters.Enqueue(enemy, StageSimulator.TurnPriority / enemy.SPD);
                enemy.InitAI();
            }

            var enemies = _enemies.Select(enemy => (Enemy) enemy.Clone()).ToList();
            var spawnWave = new SpawnWave(null, enemies, IsBoss);
            stageSimulator.Log.Add(spawnWave);
        }
    }
}
