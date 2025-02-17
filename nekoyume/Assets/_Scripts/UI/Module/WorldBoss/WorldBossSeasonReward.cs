using Nekoyume.Helper;
using Nekoyume.Model.State;

namespace Nekoyume.UI.Module.WorldBoss
{
    public class WorldBossSeasonReward : WorldBossRewardItem
    {
        public override void Reset()
        {
        }

        public void Set(RaiderState raider, int raidId)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var raidRow))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetRankingRows(raidRow.BossId, out var rankingRows))
            {
                return;
            }
        }
    }
}
