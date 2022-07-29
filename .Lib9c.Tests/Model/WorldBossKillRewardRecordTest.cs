namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using Libplanet.Assets;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class WorldBossKillRewardRecordTest
    {
        [Fact]
        public void IsClaimable()
        {
            var rewardRecord = new WorldBossKillRewardRecord();
            Assert.False(rewardRecord.IsClaimable());

            rewardRecord[0] = new List<FungibleAssetValue>();
            Assert.True(rewardRecord.IsClaimable());

            rewardRecord[0].Add(1 * CrystalCalculator.CRYSTAL);
            Assert.False(rewardRecord.IsClaimable());

            rewardRecord[1] = new List<FungibleAssetValue>();
            Assert.True(rewardRecord.IsClaimable());
        }
    }
}
