using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.Model
{
    [Serializable]
    public class SpawnWave : EventBase
    {
        public readonly int Number;
        public readonly List<Enemy> Enemies;
        public readonly bool IsBoss;
        public readonly long Exp;
        
        public SpawnWave(CharacterBase character, int number, List<Enemy> enemies, bool isBoss, long exp) : base(character)
        {
            Number = number;
            Enemies = enemies;
            IsBoss = isBoss;
            Exp = exp;
        }
        
        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpawnWave(Number, Enemies, IsBoss, Exp);
        }
    }
}
