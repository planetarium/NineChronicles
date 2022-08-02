using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Battle
{
    public class Wave
    {
        private readonly List<Enemy> _enemies = new List<Enemy>();
        public bool HasBoss;

        public void Add(Enemy enemy)
        {
            _enemies.Add(enemy);
        }

        public void Spawn(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAI();
            }

            var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
            var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
            simulator.Log.Add(spawnWave);
        }

        [Obsolete("Use Spawn")]
        public void SpawnV1(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAIV1();
            }

            var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
            var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
            simulator.Log.Add(spawnWave);
        }

        [Obsolete("Use Spawn")]
        public void SpawnV2(ISimulator simulator)
        {
            foreach (var enemy in _enemies)
            {
                simulator.Player.Targets.Add(enemy);
                simulator.Characters.Enqueue(enemy, Simulator.TurnPriority / enemy.SPD);
                enemy.InitAIV2();
            }

            var enemies = _enemies.Select(enemy => new Enemy(enemy)).ToList();
            var spawnWave = new SpawnWave(null, simulator.WaveNumber, simulator.WaveTurn, enemies, HasBoss);
            simulator.Log.Add(spawnWave);
        }
    }
}
