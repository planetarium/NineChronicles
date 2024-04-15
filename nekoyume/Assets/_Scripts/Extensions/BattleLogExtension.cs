using System.Collections.Generic;
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
                if (currentEvent is not SpawnWave spawnWave)
                {
                    continue;
                }

                foreach (var enemy in spawnWave.Enemies)
                {
                    monsterIds.Add(enemy.CharacterId);
                }
            }

            return monsterIds;
        }
    }
}
