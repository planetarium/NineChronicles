using System.Collections.Generic;
using Nekoyume.Game.Battle;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume
{
    public static class BattleLogExtension
    {
        public static HashSet<int> GetMonsterIds(this BattleLog battleLog)
        {
            var monsterIds = new HashSet<int>();
            foreach (var currentEvent in battleLog)
            {
                if (currentEvent is SpawnWave spawnWave)
                {
                    foreach (var enemy in spawnWave.Enemies)
                    {
                        monsterIds.Add(enemy.CharacterId);
                    }
                }

                if(currentEvent is SkipStageEvent skipStageEvent)
                {
                    foreach (var enemy in skipStageEvent.MonsterIds)
                    {
                        monsterIds.Add(enemy);
                    }
                }

            }

            return monsterIds;
        }
    }
}
