using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpawnWave : EventBase
    {
        public readonly List<Enemy> Enemies;
        public readonly bool HasBoss;

        public SpawnWave(CharacterBase character, List<Enemy> enemies, bool hasBoss) : base(character)
        {
            Enemies = enemies;
            HasBoss = hasBoss;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnWave(Enemies, HasBoss);
        }
    }
}
