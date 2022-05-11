namespace Lib9c.Tests.TableData
{
    using Nekoyume.TableData;
    using Xunit;

    public class StakeRegularRewardSheetTest
    {
        [Fact]
        public void SetToSheet()
        {
            const string TableContent = @"level,required_gold,item_id,rate
0,0,0,0
1,10,400000,50
1,10,500000,50";

            var sheet = new StakeRegularRewardSheet();
            sheet.Set(TableContent);

            Assert.Equal(2, sheet.Count);
            Assert.Equal(0, sheet[0].Level);
            Assert.Equal(0, sheet[0].RequiredGold);
            Assert.Single(sheet[0].Rewards);
            Assert.Equal(0, sheet[0].Rewards[0].ItemId);
            Assert.Equal(0, sheet[0].Rewards[0].Rate);

            Assert.Equal(10, sheet[1].RequiredGold);
            Assert.Equal(2, sheet[1].Rewards.Count);
            Assert.Equal(400000, sheet[1].Rewards[0].ItemId);
            Assert.Equal(50, sheet[1].Rewards[0].Rate);
            Assert.Equal(500000, sheet[1].Rewards[1].ItemId);
            Assert.Equal(50, sheet[1].Rewards[1].Rate);
        }
    }
}
