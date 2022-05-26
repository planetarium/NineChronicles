namespace Lib9c.Tests.TableData
{
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class StakeRegularRewardSheetTest
    {
        private readonly StakeRegularRewardSheet _sheet;
        private readonly Currency _currency;

        public StakeRegularRewardSheetTest()
        {
            const string TableContent = @"level,required_gold,item_id,rate
0,0,0,0
1,10,400000,50
1,10,500000,50";

            _sheet = new StakeRegularRewardSheet();
            _sheet.Set(TableContent);
            _currency = new Currency("NCG", 2, minters: null);
        }

        [Fact]
        public void SetToSheet()
        {
            Assert.Equal(2, _sheet.Count);
            Assert.Equal(0, _sheet[0].Level);
            Assert.Equal(0, _sheet[0].RequiredGold);
            Assert.Single(_sheet[0].Rewards);
            Assert.Equal(0, _sheet[0].Rewards[0].ItemId);
            Assert.Equal(0, _sheet[0].Rewards[0].Rate);

            Assert.Equal(10, _sheet[1].RequiredGold);
            Assert.Equal(2, _sheet[1].Rewards.Count);
            Assert.Equal(400000, _sheet[1].Rewards[0].ItemId);
            Assert.Equal(50, _sheet[1].Rewards[0].Rate);
            Assert.Equal(500000, _sheet[1].Rewards[1].ItemId);
            Assert.Equal(50, _sheet[1].Rewards[1].Rate);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(100, 1)]
        public void FindLevelByStakedAmount(int balance, int expectedLevel)
        {
            Assert.Equal(
                expectedLevel,
                _sheet.FindLevelByStakedAmount(default, balance * _currency)
            );
        }

        [Fact]
        public void FindLevelByStakedAmount_Throw_InsufficientBalanceException()
        {
            Assert.Throws<InsufficientBalanceException>(
                () => _sheet.FindLevelByStakedAmount(default, -1 * _currency));
        }
    }
}
