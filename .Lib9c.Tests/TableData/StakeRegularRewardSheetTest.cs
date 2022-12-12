namespace Lib9c.Tests.TableData
{
    using System;
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
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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

            Assert.All(_sheet.Values, r => Assert.Equal(StakeRegularRewardSheet.StakeRewardType.Item, r.Rewards[0].Type));

            var patchedSheet = new StakeRegularRewardSheet();
            const string tableContentWithRune = @"level,required_gold,item_id,rate,type
1,50,400000,10,Item
1,50,500000,800,Item
1,50,20001,6000,Rune
";
            patchedSheet.Set(tableContentWithRune);
            Assert.Single(patchedSheet);
            Assert.Equal(50, patchedSheet[1].RequiredGold);
            Assert.Equal(3, patchedSheet[1].Rewards.Count);
            for (int i = 0; i < 3; i++)
            {
                var reward = patchedSheet[1].Rewards[i];
                var itemId = i switch
                {
                    0 => 400000,
                    1 => 500000,
                    2 => 20001,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var rate = i switch
                {
                    0 => 10,
                    1 => 800,
                    2 => 6000,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var rewardType = i == 2
                    ? StakeRegularRewardSheet.StakeRewardType.Rune
                    : StakeRegularRewardSheet.StakeRewardType.Item;
                Assert.Equal(itemId, reward.ItemId);
                Assert.Equal(rate, reward.Rate);
                Assert.Equal(rewardType, reward.Type);
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
