namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using Nekoyume.TableData;
    using Xunit;

    public class MonsterCollectionRewardSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            var sheet = new MonsterCollectionRewardSheet();
            sheet.Set("collection_level,item_id,quantity\n1,1,1\n1,2,2");

            MonsterCollectionRewardSheet.Row row = sheet[1];
            Assert.Equal(1, row.MonsterCollectionLevel);

            List<MonsterCollectionRewardSheet.RewardInfo> rewards = row.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                MonsterCollectionRewardSheet.RewardInfo reward = rewards[i];
                Assert.Equal(i + 1, reward.ItemId);
                Assert.Equal(i + 1, reward.Quantity);
            }
        }
    }
}
