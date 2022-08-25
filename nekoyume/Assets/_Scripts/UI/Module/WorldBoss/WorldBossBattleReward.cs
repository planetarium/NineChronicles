using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossBattleReward : WorldBossRewardItem
    {
        [SerializeField]
        private List<WorldBossBattleRewardItem> individualRewardItems;

        [SerializeField]
        private List<WorldBossBattleRewardItem> killRewardItems;

        public override void Reset()
        {
            foreach (var item in individualRewardItems)
            {
                item.Reset();
            }

            foreach (var item in killRewardItems)
            {
                item.Reset();
            }
        }

        public void Set(int raidId, WorldBossRankingRecord record)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetKillRewards(row.BossId, out var rewards))
            {
                return;
            }

            foreach (var item in killRewardItems)
            {
                item.gameObject.SetActive(false);
            }

            var grade = record != null ? WorldBossHelper.CalculateRank(record.HighScore) : -1;
            rewards.Reverse();
            for (var i = 0; i < rewards.Count; i++)
            {
                var g = rewards.Count - i - 1;
                killRewardItems[i].Set(rewards[i], g == grade);
                killRewardItems[i].gameObject.SetActive(true);
            }
        }
    }
}
