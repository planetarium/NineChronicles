using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume
{
    public static class BattleLogExtension
    {
        public static List<int> GetMonsterIds(this BattleLog battleLog)
        {
            var monsterIds = new List<int>();
            foreach (var currentEvent in battleLog)
            {
                if (currentEvent is SpawnWave spawnWave)
                {
                    monsterIds.AddRange(spawnWave.Enemies.Select(enemy => enemy.CharacterId));
                }
            }

            return monsterIds;
        }
    }
}
