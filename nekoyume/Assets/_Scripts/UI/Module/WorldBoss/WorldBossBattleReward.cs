using System.Collections.Generic;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossBattleReward : WorldBossRewardItem
    {
        [SerializeField]
        private List<WorldBossBattleRewardItem> battleRewardItems;

        [SerializeField]
        private List<WorldBossBattleRewardItem> killRewardItems;

        public override void Reset()
        {
        }

        public void Set(int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var bossListRow))
            {
                return;
            }

            UpdateBattleRewards(bossListRow.BossId);
            UpdateKillRewards(bossListRow.BossId);
        }

        private void UpdateBattleRewards(int bossId)
        {

            if (!WorldBossFrontHelper.TryGetBattleRewards(bossId, out var battleRewards))
            {
                return;
            }

            foreach (var item in battleRewardItems)
            {
                item.gameObject.SetActive(false);
            }

            battleRewards.Reverse();
            for (var i = 0; i < battleRewards.Count; i++)
            {
                var g = battleRewards.Count - i - 1;
                battleRewardItems[i].Set(battleRewards[i]);
                battleRewardItems[i].gameObject.SetActive(true);
            }
        }

        private void UpdateKillRewards(int bossId)
        {
            if (!WorldBossFrontHelper.TryGetKillRewards(bossId, out var killRewards))
            {
                return;
            }

            foreach (var item in killRewardItems)
            {
                item.gameObject.SetActive(false);
            }

            killRewards.Reverse();
            for (var i = 0; i < killRewards.Count; i++)
            {
                var g = killRewards.Count - i - 1;
                killRewardItems[i].Set(killRewards[i]);
                killRewardItems[i].gameObject.SetActive(true);
            }
        }
    }
}
