namespace Lib9c.Tests.TableData
{
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class StakeActionPointCoefficientSheetTest
    {
        private readonly StakeActionPointCoefficientSheet _sheet;
        private readonly Currency _currency;

        public StakeActionPointCoefficientSheetTest()
        {
            const string tableContent = @"level,required_gold,coefficient
1,50,100
2,500,100
3,5000,80
4,50000,80
5,500000,60";

            _sheet = new StakeActionPointCoefficientSheet();
            _sheet.Set(tableContent);
            _currency = Currency.Legacy("NCG", 2, null);
        }

        [Fact]
        public void SetToSheet()
        {
            Assert.Equal(5, _sheet.Count);

            Assert.Equal(1, _sheet[1].Level);
            Assert.Equal(50, _sheet[1].RequiredGold);
            Assert.Equal(100, _sheet[1].Coefficient);

            Assert.Equal(2, _sheet[2].Level);
            Assert.Equal(500, _sheet[2].RequiredGold);
            Assert.Equal(100, _sheet[2].Coefficient);

            Assert.Equal(3, _sheet[3].Level);
            Assert.Equal(5000, _sheet[3].RequiredGold);
            Assert.Equal(80, _sheet[3].Coefficient);

            Assert.Equal(4, _sheet[4].Level);
            Assert.Equal(50000, _sheet[4].RequiredGold);
            Assert.Equal(80, _sheet[4].Coefficient);

            Assert.Equal(5, _sheet[5].Level);
            Assert.Equal(500000, _sheet[5].RequiredGold);
            Assert.Equal(60, _sheet[5].Coefficient);
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

        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 5)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 3)]
        public void GetActionPointByStaking(int level, int expectedAp)
        {
            var actual = _sheet.GetActionPointByStaking(5, 1, level);
            Assert.Equal(expectedAp, actual);
        }
    }
}
