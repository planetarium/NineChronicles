namespace Lib9c.Tests.TableData
{
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class StakeRegularFixedRewardSheetTest
    {
        private readonly StakeRegularFixedRewardSheet _sheet;
        private readonly Currency _currency;

        public StakeRegularFixedRewardSheetTest()
        {
            const string TableContent = @"level,required_gold,item_id,count
1,50,500000,1
2,500,500000,2
3,5000,500000,2
4,50000,500000,2
5,500000,500000,2";

            _sheet = new StakeRegularFixedRewardSheet();
            _sheet.Set(TableContent);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void SetToSheet()
        {
            Assert.Equal(5, _sheet.Count);

            Assert.Equal(1, _sheet[1].Level);
            Assert.Equal(50, _sheet[1].RequiredGold);
            Assert.Single(_sheet[1].Rewards);
            Assert.Equal(500000, _sheet[1].Rewards[0].ItemId);
            Assert.Equal(1, _sheet[1].Rewards[0].Count);

            Assert.Equal(2, _sheet[2].Level);
            Assert.Equal(500, _sheet[2].RequiredGold);
            Assert.Single(_sheet[2].Rewards);
            Assert.Equal(500000, _sheet[2].Rewards[0].ItemId);
            Assert.Equal(2, _sheet[2].Rewards[0].Count);

            Assert.Equal(3, _sheet[3].Level);
            Assert.Equal(5000, _sheet[3].RequiredGold);
            Assert.Single(_sheet[3].Rewards);
            Assert.Equal(500000, _sheet[3].Rewards[0].ItemId);
            Assert.Equal(2, _sheet[3].Rewards[0].Count);

            Assert.Equal(4, _sheet[4].Level);
            Assert.Equal(50000, _sheet[4].RequiredGold);
            Assert.Single(_sheet[4].Rewards);
            Assert.Equal(500000, _sheet[4].Rewards[0].ItemId);
            Assert.Equal(2, _sheet[4].Rewards[0].Count);

            Assert.Equal(5, _sheet[5].Level);
            Assert.Equal(500000, _sheet[5].RequiredGold);
            Assert.Single(_sheet[3].Rewards);
            Assert.Equal(500000, _sheet[5].Rewards[0].ItemId);
            Assert.Equal(2, _sheet[5].Rewards[0].Count);
        }

        [Theory]
        [InlineData(50, 1)]
        [InlineData(500, 2)]
        [InlineData(5000, 3)]
        [InlineData(50000, 4)]
        [InlineData(500000, 5)]
        [InlineData(100000000, 5)]
        public void FindLevelByStakedAmount(int balance, int expectedLevel)
        {
            Assert.Equal(
                expectedLevel,
                _sheet.FindLevelByStakedAmount(default, balance * _currency)
            );
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(49)]
        public void FindLevelByStakedAmount_Throw_InsufficientBalanceException(int balance)
        {
            Assert.Throws<InsufficientBalanceException>(
                () => _sheet.FindLevelByStakedAmount(default, balance * _currency));
        }
    }
}
