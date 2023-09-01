namespace Lib9c.Tests.TableData
{
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Libplanet.Action.State;
    using Libplanet.Types.Assets;
    using Nekoyume.Extensions;
    using Nekoyume.TableData;
    using Xunit;

    public class StakeRegularRewardSheetTest
    {
        private readonly StakeRegularRewardSheet _sheet;
        private readonly Currency _currency;

        public StakeRegularRewardSheetTest()
        {
            _sheet = new StakeRegularRewardSheet();
            _sheet.Set(StakeRegularRewardSheetFixtures.V1);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void SetToSheet()
        {
            Assert.Equal(5, _sheet.Count);
            var row = _sheet[1];
            Assert.Equal(50, row.RequiredGold);
            Assert.Equal(3, row.Rewards.Count);
            var reward = row.Rewards[0];
            Assert.Equal(400000, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(10m, reward.DecimalRate);
            reward = row.Rewards[1];
            Assert.Equal(500000, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(800m, reward.DecimalRate);
            reward = row.Rewards[2];
            Assert.Equal(20001, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Rune, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(6000m, reward.DecimalRate);

            row = _sheet[5];
            Assert.Equal(500000, row.RequiredGold);
            Assert.Equal(3, row.Rewards.Count);
            reward = row.Rewards[0];
            Assert.Equal(400000, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(5m, reward.DecimalRate);
            reward = row.Rewards[1];
            Assert.Equal(500000, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(800m, reward.DecimalRate);
            reward = row.Rewards[2];
            Assert.Equal(20001, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Rune, reward.Type);
            Assert.Equal(string.Empty, reward.CurrencyTicker);
            Assert.Equal(0, reward.CurrencyDecimalPlaces);
            Assert.Equal(6000m, reward.DecimalRate);
        }

        [Theory]
        [InlineData(50, 1)]
        [InlineData(500, 2)]
        [InlineData(5000, 3)]
        [InlineData(50000, 4)]
        [InlineData(500000, 5)]
        public void FindLevelByStakedAmount(int balance, int expectedLevel)
        {
            Assert.Equal(
                expectedLevel,
                _sheet.FindLevelByStakedAmount(default, balance * _currency)
            );
        }

        [Theory]
        [InlineData(49)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void FindLevelByStakedAmount_Throws_InsufficientBalanceException(int balance)
        {
            Assert.Throws<InsufficientBalanceException>(
                () => _sheet.FindLevelByStakedAmount(default, balance * _currency));
        }
    }
}
