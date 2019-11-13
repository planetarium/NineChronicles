using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Battle
{
    public class Wave
    {
        public readonly int Number;
        public readonly bool IsBoss;
        public readonly long Exp;
        
        private readonly List<Enemy> _enemies = new List<Enemy>();

        public Wave(StageSheet.WaveData waveData)
        {
            Number = waveData.Number;
            IsBoss = waveData.IsBoss;
            Exp = waveData.Exp;
        }

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
            var spawnWave = new SpawnWave(null, Number, enemies, IsBoss, Exp);
            simulator.Log.Add(spawnWave);
        }
    }
}
