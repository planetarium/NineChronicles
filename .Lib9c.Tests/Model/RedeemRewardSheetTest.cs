namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class RedeemRewardSheetTest
    {
        [Fact]
        public void RewardInfoGold()
        {
            var sheet = new RedeemRewardSheet();
            sheet.Set($"id,type,qty,item_id\n1,Gold,100");
            var row = sheet[1];

            Assert.Equal(1, row.Id);

            var rewardInfo = row.Rewards.First();

            Assert.Equal(RewardType.Gold, rewardInfo.Type);
            Assert.Equal(100, rewardInfo.Quantity);
            Assert.Null(rewardInfo.ItemId);
        }

        [Theory]
        [InlineData(10000)]
        [InlineData(40100000)]
        public void RewardInfoItem(int itemId)
        {
            var sheet = new RedeemRewardSheet();
            sheet.Set($"id,type,qty,item_id\n1,Item,1,{itemId}");
            var row = sheet[1];

            Assert.Equal(1, row.Id);

            var rewardInfo = row.Rewards.First();

            Assert.Equal(RewardType.Item, rewardInfo.Type);
            Assert.Equal(1, rewardInfo.Quantity);
            Assert.Equal(itemId, rewardInfo.ItemId);
        }

        [Fact]
        public void Serialize()
        {
            var csv = TableSheetsImporter.ImportSheets()[nameof(RedeemRewardSheet)];
            var sheet = new RedeemRewardSheet();
            sheet.Set(csv);

            var serialized = sheet.Serialize();

            Assert.IsType<Text>(serialized);

            var sheet2 = new RedeemRewardSheet();
            sheet2.Set((Text)serialized);

            Assert.Equal(sheet, sheet2);
        }
    }
}
