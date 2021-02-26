namespace Lib9c.Tests.Model
{
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaRewardSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            var weeklyArenaRewardSheet = new WeeklyArenaRewardSheet();
            weeklyArenaRewardSheet.Set("id,item_id,ratio,min,max\n1,2,0.1,0,1,1");

            var row = weeklyArenaRewardSheet[1];
            var reward = row.Reward;

            Assert.Equal(1, row.Id);
            Assert.Equal(2, reward.ItemId);
            Assert.Equal(0.1m, reward.Ratio);
            Assert.Equal(0, reward.Min);
            Assert.Equal(1, reward.Max);
            Assert.Equal(1, reward.RequiredLevel);
        }
    }
}
