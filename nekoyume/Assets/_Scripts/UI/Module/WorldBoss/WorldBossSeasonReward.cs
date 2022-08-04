using System.Collections.Generic;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossSeasonReward : WorldBossRewardItem
    {
        [SerializeField]
        private List<WorldBossBattleRewardItem> Items;

        public void Set(int raidId)
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
                // todo : select 조건 넣어줘야함
                Items[i].Set(rankingRows[i], false);
            }
        }
    }
}
