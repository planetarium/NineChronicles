namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using Nekoyume.TableData;
    using Xunit;

    public class StakingRewardSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            var sheet = new StakingRewardSheet();
            sheet.Set("staking_level,item_id,quantity\n1,1,1\n1,2,2");

            StakingRewardSheet.Row row = sheet[1];
            Assert.Equal(1, row.StakingLevel);

            List<StakingRewardSheet.RewardInfo> rewards = row.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                StakingRewardSheet.RewardInfo reward = rewards[i];
                Assert.Equal(i + 1, reward.ItemId);
                Assert.Equal(i + 1, reward.Quantity);
            }
        }
    }
}
