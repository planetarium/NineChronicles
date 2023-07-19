namespace Lib9c.Tests.TableData
{
    using System;
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
            const string TableContent = @"level,required_gold,item_id,rate
0,0,0,0
1,10,400000,50
1,10,500000,50";

            _sheet = new StakeRegularRewardSheet();
            _sheet.Set(TableContent);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
        }

        [Fact]
        public void SetToSheet()
        {
            Assert.Equal(2, _sheet.Count);
            var row = _sheet[0];
            Assert.Equal(0, row.Level);
            Assert.Equal(0, row.RequiredGold);
            Assert.Single(row.Rewards);
            var reward = row.Rewards[0];
            Assert.Equal(0, reward.ItemId);
            Assert.Equal(0, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Null(reward.CurrencyTicker);

            row = _sheet[1];
            Assert.Equal(10, row.RequiredGold);
            Assert.Equal(2, row.Rewards.Count);
            reward = row.Rewards[0];
            Assert.Equal(400000, reward.ItemId);
            Assert.Equal(50, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Null(reward.CurrencyTicker);
            reward = row.Rewards[1];
            Assert.Equal(500000, reward.ItemId);
            Assert.Equal(50, reward.Rate);
            Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, reward.Type);
            Assert.Null(reward.CurrencyTicker);

            var patchedSheet = new StakeRegularRewardSheet();
            const string tableContentWithRune =
                @"level,required_gold,item_id,rate,type,currency_ticker
1,50,400000,10,Item
1,50,500000,800,Item
1,50,20001,6000,Rune
1,50,,100,Currency,NCG
1,50,,100,Currency,CRYSTAL
1,50,,100,Currency,GARAGE
";
            patchedSheet.Set(tableContentWithRune);
            Assert.Single(patchedSheet);
            row = patchedSheet[1];
            Assert.Equal(50, row.RequiredGold);
            Assert.Equal(6, row.Rewards.Count);
            for (var i = 0; i < 6; i++)
            {
                reward = row.Rewards[i];
                var itemId = i switch
                {
                    0 => 400000,
                    1 => 500000,
                    2 => 20001,
                    3 => 0,
                    4 => 0,
                    5 => 0,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var rate = i switch
                {
                    0 => 10,
                    1 => 800,
                    2 => 6000,
                    3 => 100,
                    4 => 100,
                    5 => 100,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var rewardType = i switch
                {
                    0 => StakeRegularRewardSheet.StakeRewardType.Item,
                    1 => StakeRegularRewardSheet.StakeRewardType.Item,
                    2 => StakeRegularRewardSheet.StakeRewardType.Rune,
                    3 => StakeRegularRewardSheet.StakeRewardType.Currency,
                    4 => StakeRegularRewardSheet.StakeRewardType.Currency,
                    5 => StakeRegularRewardSheet.StakeRewardType.Currency,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var currencyTicker = i switch
                {
                    0 => null,
                    1 => null,
                    2 => null,
                    3 => "NCG",
                    4 => "CRYSTAL",
                    5 => "GARAGE",
                    _ => throw new ArgumentOutOfRangeException()
                };
                Assert.Equal(itemId, reward.ItemId);
                Assert.Equal(rate, reward.Rate);
                Assert.Equal(rewardType, reward.Type);
                Assert.Equal(currencyTicker, reward.CurrencyTicker);
            }
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
