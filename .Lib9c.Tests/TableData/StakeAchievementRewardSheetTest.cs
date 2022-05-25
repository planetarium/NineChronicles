namespace Lib9c.Tests.TableData
{
    using Libplanet.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class StakeAchievementRewardSheetTest
    {
        private readonly StakeAchievementRewardSheet _sheet;
        private readonly Currency _currency;

        public StakeAchievementRewardSheetTest()
        {
            _sheet = new TableSheets(TableSheetsImporter.ImportSheets())
                .StakeAchievementRewardSheet;
            _currency = new Currency("NCG", 2, minters: null);
        }

        [Fact]
        public void SetToSheet()
        {
            const string TableContent = @"level,required_gold,required_block_index,item_id,quantity
0,10,50400,400000,80
0,10,50400,500000,1
0,10,151200,400000,80
0,10,151200,500000,2
1,100,50400,400000,80
1,100,50400,500000,1
1,100,151200,400000,80
1,100,151200,500000,2";

            var sheet = new StakeAchievementRewardSheet();
            sheet.Set(TableContent);

            Assert.Equal(2, sheet.Count);

            Assert.Equal(2, sheet[0].Steps.Count);

            Assert.Equal(10, sheet[0].Steps[0].RequiredGold);
            Assert.Equal(50400, sheet[0].Steps[0].RequiredBlockIndex);
            Assert.Equal(2, sheet[0].Steps[0].Rewards.Count);
            Assert.Equal(400000, sheet[0].Steps[0].Rewards[0].ItemId);
            Assert.Equal(80, sheet[0].Steps[0].Rewards[0].Quantity);

            Assert.Equal(10, sheet[0].Steps[1].RequiredGold);
            Assert.Equal(151200, sheet[0].Steps[1].RequiredBlockIndex);
            Assert.Equal(2, sheet[0].Steps[1].Rewards.Count);
            Assert.Equal(500000, sheet[0].Steps[1].Rewards[1].ItemId);
            Assert.Equal(2, sheet[0].Steps[1].Rewards[1].Quantity);

            Assert.Equal(2, sheet[1].Steps.Count);

            Assert.Equal(100, sheet[1].Steps[0].RequiredGold);
            Assert.Equal(50400, sheet[1].Steps[0].RequiredBlockIndex);
            Assert.Equal(2, sheet[1].Steps[0].Rewards.Count);
            Assert.Equal(400000, sheet[1].Steps[0].Rewards[0].ItemId);
            Assert.Equal(80, sheet[1].Steps[0].Rewards[0].Quantity);

            Assert.Equal(100, sheet[1].Steps[1].RequiredGold);
            Assert.Equal(151200, sheet[1].Steps[1].RequiredBlockIndex);
            Assert.Equal(2, sheet[1].Steps[1].Rewards.Count);
            Assert.Equal(500000, sheet[0].Steps[1].Rewards[1].ItemId);
            Assert.Equal(2, sheet[0].Steps[1].Rewards[1].Quantity);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 0)]
        [InlineData(90, 0)]
        [InlineData(100, 1)]
        public void FindLevelByStakedAmount(int balance, int expectedLevel)
        {
            Assert.Equal(expectedLevel, _sheet.FindLevelByStakedAmount(balance * _currency));
        }
    }
}
