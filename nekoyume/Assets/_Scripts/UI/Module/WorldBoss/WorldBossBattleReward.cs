using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossBattleReward : WorldBossRewardItem
    {
        [SerializeField]
        private List<WorldBossBattleRewardItem> individualRewardItems;

        [SerializeField]
        private List<WorldBossBattleRewardItem> killRewardItems;

        public void Set(WorldBossKillRewardRecord record, int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetKillRewards(row.BossId, out var rewards))
            {
                return;
            }

            for (var i = 0; i < rewards.Count; i++)
            {
                // todo : select 조건 넣어줘야함
                killRewardItems[i].Set(rewards[i], false);
            }
        }
    }
}
