namespace Lib9c.Tests.Model
{
    using Nekoyume.TableData;
    using Xunit;

    public class StakingSheetTest
    {
        [Fact]
        public void Set()
        {
            var sheet = new StakingSheet();
            sheet.Set("level,required_gold,reward_id\n1,500,1");
            StakingSheet.Row row = sheet[1];
            Assert.Equal(1, row.Level);
            Assert.Equal(500, row.RequiredGold);
            Assert.Equal(1, row.RewardId);
        }
    }
}
