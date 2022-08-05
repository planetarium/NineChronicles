using System.Collections.Generic;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossSeasonReward : WorldBossRewardItem
    {
        [SerializeField]
        private List<WorldBossBattleRewardItem> Items;

        public void Set(int raidId, int myRank, int userCount)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var raidRow))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetRankingRows(raidRow.BossId, out var rankingRows))
            {
                return;
            }

            for (var i = 0; i < rankingRows.Count; i++)
            {
                Items[i].Set(rankingRows[i], myRank, userCount);
            }
        }
    }
}
