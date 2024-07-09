using System.Collections.Generic;
using Nekoyume.Game.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.BattleStatus.AdventureBoss;

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

                if(currentEvent is Breakthrough breakthroughEvent)
                {
                    foreach (var enemy in breakthroughEvent.Monsters)
                    {
                        monsterIds.Add(enemy.CharacterId);
                    }
                }

            }

            return monsterIds;
        }
    }
}
