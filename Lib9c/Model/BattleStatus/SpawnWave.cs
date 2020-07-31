using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnWave : EventBase
    {
        public readonly int WaveNumber;
        public readonly int WaveTurn;
        public readonly List<Enemy> Enemies;
        public readonly bool HasBoss;

        public SpawnWave(CharacterBase character, int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss) : base(character)
        {
            WaveNumber = waveNumber;
            WaveTurn = waveTurn;
            Enemies = enemies;
            HasBoss = hasBoss;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnWave(WaveNumber, WaveTurn, Enemies, HasBoss);
        }
    }
}
